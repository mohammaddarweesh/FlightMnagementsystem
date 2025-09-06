using FlightBooking.Application.Promotions.Services;
using FlightBooking.Domain.Promotions;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace FlightBooking.Infrastructure.Promotions.Services;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IPromotionCacheService _cacheService;
    private readonly IPromotionLockService _lockService;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(
        IPromotionRepository promotionRepository,
        IPromotionCacheService cacheService,
        IPromotionLockService lockService,
        ILogger<PromotionService> logger)
    {
        _promotionRepository = promotionRepository;
        _cacheService = cacheService;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task<PromotionValidationResult> ValidatePromotionAsync(
        string promotionCode,
        Guid? customerId,
        string? guestId,
        decimal purchaseAmount,
        string route,
        string fareClass,
        DateTime departureDate,
        DateTime bookingDate,
        bool isFirstTimeCustomer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = await GetPromotionAsync(promotionCode, cancellationToken);
            if (promotion == null)
            {
                return new PromotionValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "Promotion code not found" }
                };
            }

            return promotion.ValidateUsage(
                customerId,
                guestId,
                purchaseAmount,
                route,
                fareClass,
                departureDate,
                bookingDate,
                isFirstTimeCustomer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promotion {PromotionCode}", promotionCode);
            return new PromotionValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Error validating promotion code" }
            };
        }
    }

    public async Task<PromotionUsageResult> RecordPromotionUsageAsync(
        string promotionCode,
        Guid? customerId,
        string? guestId,
        Guid bookingId,
        decimal purchaseAmount,
        decimal discountAmount,
        string usedBy,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var lockTimeout = TimeSpan.FromSeconds(30);
        using var promotionLock = await _lockService.AcquireLockAsync(promotionCode, lockTimeout, cancellationToken);
        
        if (promotionLock == null)
        {
            throw new PromotionConcurrencyException(promotionCode, "record_usage", 
                "Could not acquire lock for promotion usage recording");
        }

        try
        {
            var promotion = await _promotionRepository.GetByCodeAsync(promotionCode, cancellationToken);
            if (promotion == null)
            {
                return new PromotionUsageResult
                {
                    Success = false,
                    ErrorMessage = "Promotion not found"
                };
            }

            // Double-check usage limits with fresh data
            if (promotion.HasReachedMaxUsage)
            {
                return new PromotionUsageResult
                {
                    Success = false,
                    ErrorMessage = "Promotion usage limit has been reached"
                };
            }

            // Check customer-specific limits
            if (customerId.HasValue && promotion.MaxUsagePerCustomer.HasValue)
            {
                var customerUsageCount = await _promotionRepository.GetCustomerUsageCountAsync(
                    promotionCode, customerId, guestId, cancellationToken);
                
                if (customerUsageCount >= promotion.MaxUsagePerCustomer.Value)
                {
                    return new PromotionUsageResult
                    {
                        Success = false,
                        ErrorMessage = $"Customer usage limit of {promotion.MaxUsagePerCustomer.Value} reached"
                    };
                }
            }

            // Check daily usage limits
            if (promotion.MaxUsagePerDay.HasValue)
            {
                var dailyUsageCount = await _promotionRepository.GetDailyUsageCountAsync(
                    promotionCode, DateTime.Today, cancellationToken);
                
                if (dailyUsageCount >= promotion.MaxUsagePerDay.Value)
                {
                    return new PromotionUsageResult
                    {
                        Success = false,
                        ErrorMessage = "Daily usage limit reached"
                    };
                }
            }

            // Record the usage atomically
            var usage = promotion.RecordUsage(customerId, guestId, bookingId, purchaseAmount, discountAmount, usedBy);
            
            var success = await _promotionRepository.TryRecordUsageAsync(promotion, usage, cancellationToken);
            if (!success)
            {
                throw new PromotionConcurrencyException(promotionCode, "record_usage", 
                    "Failed to record usage due to concurrency conflict");
            }

            // Update cache
            await _cacheService.SetPromotionAsync(promotion, TimeSpan.FromMinutes(15), cancellationToken);
            await _cacheService.IncrementUsageCountAsync(promotionCode, "total", cancellationToken);
            
            if (customerId.HasValue)
            {
                await _cacheService.IncrementUsageCountAsync(promotionCode, $"customer:{customerId}", cancellationToken);
            }

            _logger.LogInformation("Recorded promotion usage: {PromotionCode} for booking {BookingId} by {UsedBy}", 
                promotionCode, bookingId, usedBy);

            return new PromotionUsageResult
            {
                Success = true,
                UsageId = usage.Id,
                DiscountApplied = discountAmount,
                RemainingUsage = promotion.RemainingUsage,
                UsedAt = usage.UsedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording promotion usage for {PromotionCode}", promotionCode);
            throw;
        }
    }

    public async Task<PromotionApplicationResult> ValidateAndApplyPromotionAsync(
        string promotionCode,
        Guid? customerId,
        string? guestId,
        Guid bookingId,
        decimal purchaseAmount,
        string route,
        string fareClass,
        DateTime departureDate,
        DateTime bookingDate,
        bool isFirstTimeCustomer,
        string usedBy,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var lockTimeout = TimeSpan.FromSeconds(30);
        using var promotionLock = await _lockService.AcquireLockAsync(promotionCode, lockTimeout, cancellationToken);
        
        if (promotionLock == null)
        {
            throw new PromotionConcurrencyException(promotionCode, "validate_and_apply", 
                "Could not acquire lock for promotion validation and application");
        }

        using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        
        try
        {
            // Step 1: Validate the promotion
            var validationResult = await ValidatePromotionAsync(
                promotionCode, customerId, guestId, purchaseAmount, route, fareClass,
                departureDate, bookingDate, isFirstTimeCustomer, cancellationToken);

            if (!validationResult.IsValid)
            {
                return new PromotionApplicationResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validationResult.Errors),
                    Warnings = validationResult.Warnings
                };
            }

            // Step 2: Calculate discount
            var promotion = await _promotionRepository.GetByCodeAsync(promotionCode, cancellationToken);
            var discountAmount = promotion!.CalculateDiscount(purchaseAmount);
            var finalAmount = purchaseAmount - discountAmount;

            // Step 3: Record usage
            var usageResult = await RecordPromotionUsageAsync(
                promotionCode, customerId, guestId, bookingId, purchaseAmount, discountAmount,
                usedBy, ipAddress, userAgent, cancellationToken);

            if (!usageResult.Success)
            {
                return new PromotionApplicationResult
                {
                    Success = false,
                    ErrorMessage = usageResult.ErrorMessage,
                    Warnings = validationResult.Warnings
                };
            }

            transaction.Complete();

            _logger.LogInformation("Successfully applied promotion {PromotionCode} to booking {BookingId}: {DiscountAmount:C} discount", 
                promotionCode, bookingId, discountAmount);

            return new PromotionApplicationResult
            {
                Success = true,
                UsageId = usageResult.UsageId,
                DiscountApplied = discountAmount,
                FinalAmount = finalAmount,
                RemainingUsage = usageResult.RemainingUsage,
                UsedAt = usageResult.UsedAt,
                Terms = validationResult.Terms,
                Warnings = validationResult.Warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying promotion {PromotionCode} to booking {BookingId}", promotionCode, bookingId);
            throw;
        }
    }

    public async Task<bool> ReversePromotionUsageAsync(
        Guid bookingId,
        string reversedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _promotionRepository.ReverseUsageAsync(bookingId, reversedBy, reason, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Reversed promotion usage for booking {BookingId} by {ReversedBy}. Reason: {Reason}", 
                    bookingId, reversedBy, reason);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reversing promotion usage for booking {BookingId}", bookingId);
            return false;
        }
    }

    public async Task<List<AvailablePromotionDto>> GetAvailablePromotionsAsync(
        Guid? customerId,
        string? guestId,
        string route,
        string fareClass,
        DateTime departureDate,
        decimal? estimatedAmount = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var promotions = await _promotionRepository.GetPromotionsForCustomerAsync(customerId, guestId, cancellationToken);
            var availablePromotions = new List<AvailablePromotionDto>();

            foreach (var promotion in promotions)
            {
                if (!promotion.IsCurrentlyValid || promotion.HasReachedMaxUsage)
                    continue;

                // Quick validation without detailed checks
                var isApplicable = true;

                if (promotion.ApplicableRoutes.Any() && !promotion.ApplicableRoutes.Contains(route))
                    isApplicable = false;

                if (promotion.ExcludedRoutes.Contains(route))
                    isApplicable = false;

                if (promotion.ApplicableFareClasses.Any() && !promotion.ApplicableFareClasses.Contains(fareClass))
                    isApplicable = false;

                if (promotion.ExcludedFareClasses.Contains(fareClass))
                    isApplicable = false;

                if (promotion.ApplicableDaysOfWeek.Any() && !promotion.ApplicableDaysOfWeek.Contains(departureDate.DayOfWeek))
                    isApplicable = false;

                if (!isApplicable)
                    continue;

                var estimatedDiscount = estimatedAmount.HasValue ? promotion.CalculateDiscount(estimatedAmount.Value) : 0;

                availablePromotions.Add(new AvailablePromotionDto
                {
                    Code = promotion.Code,
                    Name = promotion.Name,
                    Description = promotion.Description,
                    Type = promotion.Type,
                    Value = promotion.Value,
                    EstimatedDiscount = estimatedDiscount,
                    ValidTo = promotion.ValidTo,
                    RemainingUsage = promotion.RemainingUsage == int.MaxValue ? null : promotion.RemainingUsage,
                    RequiresMinimumPurchase = promotion.MinPurchaseAmount.HasValue,
                    MinimumPurchaseAmount = promotion.MinPurchaseAmount,
                    Terms = promotion.Terms?.Split('\n').ToList() ?? new List<string>(),
                    MarketingMessage = promotion.MarketingMessage
                });
            }

            return availablePromotions.OrderByDescending(p => p.EstimatedDiscount).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available promotions for customer {CustomerId}", customerId);
            return new List<AvailablePromotionDto>();
        }
    }

    public async Task<PromotionUsageStatistics> GetPromotionUsageStatisticsAsync(
        string promotionCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = await _promotionRepository.GetByCodeAsync(promotionCode, cancellationToken);
            if (promotion == null)
            {
                throw new PromotionNotFoundException(promotionCode);
            }

            var usageHistory = await _promotionRepository.GetUsageHistoryAsync(promotionCode, cancellationToken: cancellationToken);

            return new PromotionUsageStatistics
            {
                PromotionCode = promotion.Code,
                PromotionName = promotion.Name,
                TotalUsage = promotion.CurrentTotalUsage,
                MaxUsage = promotion.MaxTotalUsage,
                RemainingUsage = promotion.RemainingUsage,
                UsagePercentage = promotion.UsagePercentage,
                TotalDiscountGiven = usageHistory.Sum(u => u.DiscountAmount),
                AverageDiscountPerUsage = usageHistory.Any() ? usageHistory.Average(u => u.DiscountAmount) : 0,
                FirstUsedAt = usageHistory.MinBy(u => u.UsedAt)?.UsedAt,
                LastUsedAt = usageHistory.MaxBy(u => u.UsedAt)?.UsedAt,
                UniqueCustomers = usageHistory.Where(u => u.CustomerId.HasValue).Select(u => u.CustomerId).Distinct().Count(),
                UniqueGuests = usageHistory.Where(u => !string.IsNullOrEmpty(u.GuestId)).Select(u => u.GuestId).Distinct().Count(),
                IsActive = promotion.IsActive,
                ValidFrom = promotion.ValidFrom,
                ValidTo = promotion.ValidTo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotion usage statistics for {PromotionCode}", promotionCode);
            throw;
        }
    }

    public async Task<bool> HasCustomerReachedUsageLimitAsync(
        string promotionCode,
        Guid? customerId,
        string? guestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = await GetPromotionAsync(promotionCode, cancellationToken);
            if (promotion == null || !promotion.MaxUsagePerCustomer.HasValue)
                return false;

            var usageCount = await _promotionRepository.GetCustomerUsageCountAsync(promotionCode, customerId, guestId, cancellationToken);
            return usageCount >= promotion.MaxUsagePerCustomer.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking customer usage limit for promotion {PromotionCode}", promotionCode);
            return true; // Err on the side of caution
        }
    }

    public async Task<List<CustomerPromotionUsageDto>> GetCustomerPromotionHistoryAsync(
        Guid? customerId,
        string? guestId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var usageHistory = await _promotionRepository.GetCustomerUsageHistoryAsync(customerId, guestId, fromDate, toDate, cancellationToken);
            
            return usageHistory.Select(u => new CustomerPromotionUsageDto
            {
                UsageId = u.Id,
                PromotionCode = u.Promotion.Code,
                PromotionName = u.Promotion.Name,
                BookingId = u.BookingId,
                PurchaseAmount = u.PurchaseAmount,
                DiscountAmount = u.DiscountAmount,
                UsedAt = u.UsedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer promotion history for customer {CustomerId}", customerId);
            return new List<CustomerPromotionUsageDto>();
        }
    }

    private async Task<Promotion?> GetPromotionAsync(string promotionCode, CancellationToken cancellationToken)
    {
        // Try cache first
        var cachedPromotion = await _cacheService.GetPromotionAsync(promotionCode, cancellationToken);
        if (cachedPromotion != null)
            return cachedPromotion;

        // Fallback to repository
        var promotion = await _promotionRepository.GetByCodeAsync(promotionCode, cancellationToken);
        if (promotion != null)
        {
            await _cacheService.SetPromotionAsync(promotion, TimeSpan.FromMinutes(15), cancellationToken);
        }

        return promotion;
    }
}

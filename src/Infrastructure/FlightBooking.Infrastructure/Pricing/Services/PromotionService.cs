using FlightBooking.Application.Pricing.Services;
using FlightBooking.Application.Pricing.Strategies;
using FlightBooking.Domain.Pricing;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Pricing.Services;

public class PromotionService : IPromotionService
{
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(ILogger<PromotionService> logger)
    {
        _logger = logger;
    }

    public Task<List<AvailablePromotion>> GetAvailablePromotionsAsync(string route, DateTime departureDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var promotions = new List<AvailablePromotion>();

            // Sample promotions - in real implementation, these would come from database
            var currentPromotions = GetCurrentPromotions();

            foreach (var promo in currentPromotions)
            {
                if (IsPromotionApplicable(promo, route, departureDate))
                {
                    promotions.Add(new AvailablePromotion
                    {
                        Code = promo.Code,
                        Name = promo.Name,
                        Description = promo.Description,
                        EstimatedSavings = EstimateSavings(promo, 500m), // Estimate based on average fare
                        ExpiryDate = promo.ValidTo,
                        Terms = GetPromotionTerms(promo),
                        RequiresMinimumPurchase = promo.MinPurchaseAmount.HasValue,
                        MinimumPurchaseAmount = promo.MinPurchaseAmount
                    });
                }
            }

            _logger.LogDebug("Found {PromotionCount} available promotions for route {Route} on {Date}", 
                promotions.Count, route, departureDate);

            return Task.FromResult(promotions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available promotions for route {Route}", route);
            return Task.FromResult(new List<AvailablePromotion>());
        }
    }

    public Task<PromoCodeValidationResult> ValidatePromoCodeAsync(string promoCode, FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = GetCurrentPromotions().FirstOrDefault(p => 
                p.Code.Equals(promoCode, StringComparison.OrdinalIgnoreCase));

            if (promotion == null)
            {
                return Task.FromResult(new PromoCodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Promotional code not found"
                });
            }

            if (!promotion.IsActive)
            {
                return Task.FromResult(new PromoCodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Promotional code is no longer active"
                });
            }

            if (DateTime.UtcNow < promotion.ValidFrom || DateTime.UtcNow > promotion.ValidTo)
            {
                return Task.FromResult(new PromoCodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Promotional code is not valid during this period. Valid from {promotion.ValidFrom:MMM dd} to {promotion.ValidTo:MMM dd}"
                });
            }

            var restrictions = new List<string>();

            // Check route restrictions
            if (promotion.ApplicableRoutes.Any() && !promotion.ApplicableRoutes.Contains(request.Route))
            {
                restrictions.Add($"Not valid for route {request.Route}");
            }

            // Check minimum purchase
            if (promotion.MinPurchaseAmount.HasValue && request.BaseFare < promotion.MinPurchaseAmount.Value)
            {
                restrictions.Add($"Minimum purchase of {promotion.MinPurchaseAmount.Value:C} required");
            }

            // Check advance purchase requirements
            if (promotion.MinAdvanceDays.HasValue)
            {
                var daysUntilDeparture = (request.DepartureDate - request.BookingDate).TotalDays;
                if (daysUntilDeparture < promotion.MinAdvanceDays.Value)
                {
                    restrictions.Add($"Must be booked at least {promotion.MinAdvanceDays.Value} days in advance");
                }
            }

            // Check usage limits
            if (promotion.MaxUsageCount.HasValue && promotion.CurrentUsageCount >= promotion.MaxUsageCount.Value)
            {
                restrictions.Add("Promotional code usage limit reached");
            }

            var isValid = !restrictions.Any();
            var estimatedDiscount = isValid ? CalculatePromotionDiscountAsync(promoCode, request.BaseFare, request, cancellationToken).Result : 0m;

            return Task.FromResult(new PromoCodeValidationResult
            {
                IsValid = isValid,
                ErrorMessage = isValid ? null : string.Join("; ", restrictions),
                EstimatedDiscount = estimatedDiscount,
                Terms = GetPromotionTerms(promotion),
                ExpiryDate = promotion.ValidTo,
                Restrictions = restrictions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promo code {PromoCode}", promoCode);
            return Task.FromResult(new PromoCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "Error validating promotional code"
            });
        }
    }

    public Task<decimal> CalculatePromotionDiscountAsync(string promoCode, decimal baseFare, FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = GetCurrentPromotions().FirstOrDefault(p => 
                p.Code.Equals(promoCode, StringComparison.OrdinalIgnoreCase));

            if (promotion == null || !promotion.IsActive)
            {
                return Task.FromResult(0m);
            }

            var discount = promotion.Type switch
            {
                PromotionType.Percentage => baseFare * (promotion.Value / 100m),
                PromotionType.FixedAmount => promotion.Value,
                PromotionType.BuyOneGetOne => baseFare * 0.5m,
                _ => 0m
            };

            // Apply maximum discount limit
            if (promotion.MaxDiscount.HasValue && discount > promotion.MaxDiscount.Value)
            {
                discount = promotion.MaxDiscount.Value;
            }

            // Ensure discount doesn't exceed base fare
            if (discount > baseFare)
            {
                discount = baseFare;
            }

            return Task.FromResult(Math.Round(discount, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating promotion discount for code {PromoCode}", promoCode);
            return Task.FromResult(0m);
        }
    }

    private List<Promotion> GetCurrentPromotions()
    {
        // In real implementation, this would come from database
        return new List<Promotion>
        {
            new()
            {
                Code = "WELCOME10",
                Name = "Welcome Discount",
                Description = "10% off for new customers",
                Type = PromotionType.Percentage,
                Value = 10m,
                ValidFrom = DateTime.Today.AddDays(-30),
                ValidTo = DateTime.Today.AddDays(60),
                IsActive = true,
                MinPurchaseAmount = 100m,
                IsFirstTimeCustomerOnly = true
            },
            new()
            {
                Code = "SUMMER25",
                Name = "Summer Sale",
                Description = "25% off summer travel",
                Type = PromotionType.Percentage,
                Value = 25m,
                ValidFrom = new DateTime(2025, 6, 1),
                ValidTo = new DateTime(2025, 8, 31),
                IsActive = true,
                ApplicableRoutes = new List<string> { "JFK-LAX", "NYC-MIA", "SFO-SEA" },
                MaxDiscount = 200m
            },
            new()
            {
                Code = "FLAT50",
                Name = "Flat $50 Off",
                Description = "Save $50 on any booking",
                Type = PromotionType.FixedAmount,
                Value = 50m,
                ValidFrom = DateTime.Today.AddDays(-10),
                ValidTo = DateTime.Today.AddDays(20),
                IsActive = true,
                MinPurchaseAmount = 200m
            },
            new()
            {
                Code = "EARLYBIRD",
                Name = "Early Bird Special",
                Description = "15% off for bookings 30+ days in advance",
                Type = PromotionType.Percentage,
                Value = 15m,
                ValidFrom = DateTime.Today.AddDays(-60),
                ValidTo = DateTime.Today.AddDays(90),
                IsActive = true,
                MinAdvanceDays = 30,
                MaxDiscount = 150m
            }
        };
    }

    private bool IsPromotionApplicable(Promotion promotion, string route, DateTime departureDate)
    {
        // Check if promotion is currently valid
        if (!promotion.IsActive || DateTime.UtcNow < promotion.ValidFrom || DateTime.UtcNow > promotion.ValidTo)
        {
            return false;
        }

        // Check route restrictions
        if (promotion.ApplicableRoutes.Any() && !promotion.ApplicableRoutes.Contains(route))
        {
            return false;
        }

        // Check usage limits
        if (promotion.MaxUsageCount.HasValue && promotion.CurrentUsageCount >= promotion.MaxUsageCount.Value)
        {
            return false;
        }

        return true;
    }

    private decimal EstimateSavings(Promotion promotion, decimal estimatedFare)
    {
        var discount = promotion.Type switch
        {
            PromotionType.Percentage => estimatedFare * (promotion.Value / 100m),
            PromotionType.FixedAmount => promotion.Value,
            PromotionType.BuyOneGetOne => estimatedFare * 0.5m,
            _ => 0m
        };

        if (promotion.MaxDiscount.HasValue && discount > promotion.MaxDiscount.Value)
        {
            discount = promotion.MaxDiscount.Value;
        }

        return Math.Round(discount, 2);
    }

    private List<string> GetPromotionTerms(Promotion promotion)
    {
        var terms = new List<string>
        {
            $"Valid from {promotion.ValidFrom:MMM dd, yyyy} to {promotion.ValidTo:MMM dd, yyyy}",
            "Cannot be combined with other offers"
        };

        if (promotion.MinPurchaseAmount.HasValue)
        {
            terms.Add($"Minimum purchase of {promotion.MinPurchaseAmount.Value:C} required");
        }

        if (promotion.MaxDiscount.HasValue)
        {
            terms.Add($"Maximum discount of {promotion.MaxDiscount.Value:C}");
        }

        if (promotion.ApplicableRoutes.Any())
        {
            terms.Add($"Valid only for routes: {string.Join(", ", promotion.ApplicableRoutes)}");
        }

        if (promotion.MinAdvanceDays.HasValue)
        {
            terms.Add($"Must be booked at least {promotion.MinAdvanceDays.Value} days in advance");
        }

        if (promotion.IsFirstTimeCustomerOnly)
        {
            terms.Add("Valid for new customers only");
        }

        return terms;
    }
}

using FlightBooking.Application.Bookings.Commands;
using FlightBooking.Application.Bookings.Queries;
using FlightBooking.Application.Bookings.Services;
using FlightBooking.Domain.Bookings;
using FlightBooking.Infrastructure.Redis.Keys;
using FlightBooking.Infrastructure.Redis.Services;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Bookings.Services;

/// <summary>
/// Redis-enhanced booking service demonstrating professional Redis integration
/// This is a decorator pattern implementation that adds Redis capabilities
/// TODO: Enable when lock services are available
/// </summary>
/*
public class RedisEnhancedBookingService
{
    private readonly IBookingService _baseService;
    private readonly IRedisCacheService _cacheService;
    private readonly ISeatLockService _seatLockService;
    private readonly IPromotionLockService _promotionLockService;
    private readonly IRedisSessionService _sessionService;
    private readonly IRedisMetricsService _metricsService;
    private readonly ILogger<RedisEnhancedBookingService> _logger;

    public RedisEnhancedBookingService(
        IBookingService baseService,
        IRedisCacheService cacheService,
        ISeatLockService seatLockService,
        IPromotionLockService promotionLockService,
        IRedisSessionService sessionService,
        IRedisMetricsService metricsService,
        ILogger<RedisEnhancedBookingService> logger)
    {
        _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _seatLockService = seatLockService ?? throw new ArgumentNullException(nameof(seatLockService));
        _promotionLockService = promotionLockService ?? throw new ArgumentNullException(nameof(promotionLockService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CreateBookingResult> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Check for cached idempotent result first
            var idempotencyKey = $"create-booking:{command.IdempotencyKey}";
            var cachedResult = await _cacheService.GetAsync<CreateBookingResult>(idempotencyKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Returning cached booking creation result for idempotency key {Key}", command.IdempotencyKey);
                await _metricsService.RecordCacheHitAsync("booking-creation");
                return cachedResult;
            }

            await _metricsService.RecordCacheMissAsync("booking-creation");

            // Handle promotion code with distributed locking
            CreateBookingResult result;
            if (!string.IsNullOrEmpty(command.PromoCode))
            {
                result = await CreateBookingWithPromotionAsync(command);
            }
            else
            {
                result = await _baseService.CreateBookingAsync(command, cancellationToken);
            }

            // Cache the result for idempotency
            if (result.Success)
            {
                await _cacheService.SetAsync(idempotencyKey, result, RedisTtl.VeryLongCache);
                
                // Cache booking details for quick retrieval
                var bookingCacheKey = _cacheService.BookingCacheKey(result.BookingId!.Value);
                await _cacheService.SetAsync(bookingCacheKey, result, RedisTtl.MediumCache);
            }

            stopwatch.Stop();
            await _metricsService.RecordOperationDurationAsync("create-booking", stopwatch.Elapsed);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await _metricsService.RecordErrorAsync("create-booking", ex.GetType().Name);
            _logger.LogError(ex, "Error creating booking with Redis enhancement");
            throw;
        }
    }

    public async Task<ModifyBookingResult> ModifyBookingAsync(ModifyBookingCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Handle seat changes with distributed locking
            if (command.ModificationType == BookingModificationType.SeatChanged)
            {
                return await ModifyBookingWithSeatLockAsync(command, cancellationToken);
            }

            var result = await _baseService.ModifyBookingAsync(command, cancellationToken);

            // Invalidate cached booking data
            if (result.Success)
            {
                var bookingCacheKey = _cacheService.BookingCacheKey(command.BookingId);
                await _cacheService.DeleteAsync(bookingCacheKey);
                
                // Invalidate related cache entries
                await _cacheService.InvalidateTagAsync($"booking:{command.BookingId}");
            }

            stopwatch.Stop();
            await _metricsService.RecordOperationDurationAsync("modify-booking", stopwatch.Elapsed);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await _metricsService.RecordErrorAsync("modify-booking", ex.GetType().Name);
            _logger.LogError(ex, "Error modifying booking with Redis enhancement");
            throw;
        }
    }

    public async Task<CancelBookingResult> CancelBookingAsync(CancelBookingCommand command)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await _baseService.CancelBookingAsync(command);

            // Invalidate cached booking data
            if (result.Success)
            {
                var bookingCacheKey = _cacheService.BookingCacheKey(command.BookingId);
                await _cacheService.DeleteAsync(bookingCacheKey);
                
                // Release any seat locks for this booking
                // Note: In a real implementation, you'd get the flight ID and seats from the booking
                // await _seatLockService.ReleaseSeatLocksForFlightAsync(flightId);
            }

            stopwatch.Stop();
            await _metricsService.RecordOperationDurationAsync("cancel-booking", stopwatch.Elapsed);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await _metricsService.RecordErrorAsync("cancel-booking", ex.GetType().Name);
            _logger.LogError(ex, "Error cancelling booking with Redis enhancement");
            throw;
        }
    }

    public async Task<ConfirmBookingResult> ConfirmBookingAsync(ConfirmBookingCommand command)
    {
        var result = await _baseService.ConfirmBookingAsync(command);

        // Update cached booking data
        if (result.Success)
        {
            var bookingCacheKey = _cacheService.BookingCacheKey(command.BookingId);
            await _cacheService.DeleteAsync(bookingCacheKey); // Force refresh on next access
        }

        return result;
    }

    public async Task<CheckInResult> CheckInAsync(CheckInCommand command)
    {
        var result = await _baseService.CheckInAsync(command);

        // Update cached booking data
        if (result.Success)
        {
            var bookingCacheKey = _cacheService.BookingCacheKey(command.BookingId);
            await _cacheService.DeleteAsync(bookingCacheKey); // Force refresh on next access
        }

        return result;
    }

    public async Task<BookingModificationValidationResult> ValidateModificationAsync(ValidateBookingModificationQuery query)
    {
        // Cache validation results for a short time
        var validationCacheKey = $"validation:{query.BookingId}:{query.ModificationType}:{GetQueryHash(query)}";
        
        return await _cacheService.GetOrSetAsync(validationCacheKey, async () =>
        {
            return await _baseService.ValidateModificationAsync(query);
        }, RedisTtl.ShortCache);
    }

    public async Task<CancellationCalculationResult> CalculateCancellationAsync(Guid bookingId, CancellationReason reason)
    {
        // Cache cancellation calculations
        var calculationCacheKey = $"cancellation-calc:{bookingId}:{reason}";
        
        return await _cacheService.GetOrSetAsync(calculationCacheKey, async () =>
        {
            return await _baseService.CalculateCancellationAsync(bookingId, reason);
        }, RedisTtl.ShortCache);
    }

    public async Task<int> ExpireBookingsAsync()
    {
        return await _baseService.ExpireBookingsAsync();
    }

    public async Task<int> ProcessPendingPaymentsAsync()
    {
        return await _baseService.ProcessPendingPaymentsAsync();
    }

    public async Task<int> SendBookingRemindersAsync()
    {
        return await _baseService.SendBookingRemindersAsync();
    }

    #region Private Methods

    private async Task<CreateBookingResult> CreateBookingWithPromotionAsync(CreateBookingCommand command)
    {
        _logger.LogDebug("Creating booking with promotion code {PromoCode}", command.PromoCode);

        // Use distributed lock to prevent double redemption
        return await _promotionLockService.ExecuteWithPromotionLockAsync(
            command.PromoCode!, 
            async () =>
            {
                // Check promotion usage in cache first
                var promotionCacheKey = _cacheService.PromotionCacheKey(command.PromoCode!);
                var promotionData = await _cacheService.GetAsync<object>(promotionCacheKey);
                
                if (promotionData == null)
                {
                    // Load promotion data and cache it
                    // In a real implementation, this would load from database
                    await _cacheService.SetAsync(promotionCacheKey, new { Code = command.PromoCode, UsageCount = 0 }, RedisTtl.MediumCache);
                }

                return await _baseService.CreateBookingAsync(command);
            },
            command.CustomerId);
    }

    private async Task<ModifyBookingResult> ModifyBookingWithSeatLockAsync(ModifyBookingCommand command)
    {
        _logger.LogDebug("Modifying booking {BookingId} with seat change", command.BookingId);

        // Extract seat information from modification data
        if (command.ModificationData.TryGetValue("flight_id", out var flightIdObj) &&
            command.ModificationData.TryGetValue("new_seat", out var newSeatObj) &&
            Guid.TryParse(flightIdObj.ToString(), out var flightId))
        {
            var newSeat = newSeatObj.ToString()!;

            // Use distributed lock for seat allocation
            return await _seatLockService.ExecuteWithSeatLockAsync(flightId, newSeat, async () =>
            {
                // Check seat availability
                var isLocked = await _seatLockService.IsSeatLockedAsync(flightId, newSeat);
                if (isLocked)
                {
                    throw new InvalidOperationException($"Seat {newSeat} is currently being allocated by another process");
                }

                return await _baseService.ModifyBookingAsync(command);
            });
        }

        // Fall back to base service for non-seat modifications
        return await _baseService.ModifyBookingAsync(command);
    }

    private static string GetQueryHash(ValidateBookingModificationQuery query)
    {
        // Simple hash for caching validation results
        var data = $"{query.BookingId}:{query.ModificationType}:{string.Join(",", query.ModificationData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data))[..16];
    }

    #endregion
}

/// <summary>
/// Extension methods for Redis-enhanced booking operations
/// </summary>
public static class RedisBookingExtensions
{
    /// <summary>
    /// Create a guest session for booking process
    /// </summary>
    public static async Task<string> CreateGuestBookingSessionAsync(
        this IRedisSessionService sessionService,
        string? preferredCurrency = "USD",
        string? preferredLanguage = "en-US")
    {
        var sessionId = await sessionService.CreateSessionAsync(isAuthenticated: false);
        
        // Store minimal guest preferences
        await sessionService.SetSessionDataAsync(sessionId, "preferences", new
        {
            Currency = preferredCurrency,
            Language = preferredLanguage,
            CreatedAt = DateTime.UtcNow
        });

        return sessionId;
    }

    /// <summary>
    /// Store booking progress in session
    /// </summary>
    public static async Task<bool> StoreBookingProgressAsync(
        this IRedisSessionService sessionService,
        string sessionId,
        object bookingData)
    {
        return await sessionService.SetSessionDataAsync(sessionId, "booking_progress", bookingData);
    }

    /// <summary>
    /// Get booking progress from session
    /// </summary>
    public static async Task<T?> GetBookingProgressAsync<T>(
        this IRedisSessionService sessionService,
        string sessionId) where T : class
    {
        return await sessionService.GetSessionDataAsync<T>(sessionId, "booking_progress");
    }

    /// <summary>
    /// Clear booking progress from session
    /// </summary>
    public static async Task<bool> ClearBookingProgressAsync(
        this IRedisSessionService sessionService,
        string sessionId)
    {
        return await sessionService.RemoveSessionDataAsync(sessionId, "booking_progress");
    }
}
*/

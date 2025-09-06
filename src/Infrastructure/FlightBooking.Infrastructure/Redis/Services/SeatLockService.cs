using FlightBooking.Infrastructure.Redis.Configuration;
using FlightBooking.Infrastructure.Redis.Keys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedLockNet;

namespace FlightBooking.Infrastructure.Redis.Services;

/// <summary>
/// Specialized lock service for seat allocation with optimized operations
/// TODO: Enable when DistributedLockService is fixed
/// </summary>
/*
public class SeatLockService : ISeatLockService
{
    private readonly IDistributedLockService _lockService;
    private readonly IRedisService _redisService;
    private readonly ILogger<SeatLockService> _logger;
    private readonly RedisConfiguration _config;

    public SeatLockService(
        IDistributedLockService lockService,
        IRedisService redisService,
        IOptions<RedisConfiguration> config,
        ILogger<SeatLockService> logger)
    {
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IRedLock?> AcquireSeatLockAsync(Guid flightId, string seatNumber, TimeSpan? timeout = null)
    {
        var resource = BuildSeatResource(flightId, seatNumber);
        var lockTimeout = timeout ?? RedisTtl.MediumLock;

        _logger.LogDebug("Attempting to acquire seat lock for flight {FlightId}, seat {SeatNumber}", flightId, seatNumber);

        try
        {
            var redLock = await _lockService.AcquireLockAsync(resource, lockTimeout);
            
            if (redLock?.IsAcquired == true)
            {
                // Store seat lock metadata for tracking
                await StoreSeatLockMetadataAsync(flightId, seatNumber, redLock);
                _logger.LogDebug("Successfully acquired seat lock for flight {FlightId}, seat {SeatNumber}", flightId, seatNumber);
            }
            else
            {
                _logger.LogWarning("Failed to acquire seat lock for flight {FlightId}, seat {SeatNumber}", flightId, seatNumber);
            }

            return redLock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring seat lock for flight {FlightId}, seat {SeatNumber}", flightId, seatNumber);
            throw;
        }
    }

    public async Task<bool> IsSeatLockedAsync(Guid flightId, string seatNumber)
    {
        var resource = BuildSeatResource(flightId, seatNumber);
        
        try
        {
            return await _lockService.IsLockedAsync(resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking seat lock status for flight {FlightId}, seat {SeatNumber}", flightId, seatNumber);
            return false;
        }
    }

    public async Task<T> ExecuteWithSeatLockAsync<T>(Guid flightId, string seatNumber, Func<Task<T>> action)
    {
        var resource = BuildSeatResource(flightId, seatNumber);
        
        _logger.LogDebug("Executing action with seat lock for flight {FlightId}, seat {SeatNumber}", flightId, seatNumber);

        try
        {
            return await _lockService.ExecuteWithLockAsync(resource, action, RedisTtl.MediumLock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action with seat lock for flight {FlightId}, seat {SeatNumber}", flightId, seatNumber);
            throw;
        }
    }

    public async Task<Dictionary<string, bool>> GetSeatLockStatusAsync(Guid flightId, IEnumerable<string> seatNumbers)
    {
        var result = new Dictionary<string, bool>();
        
        try
        {
            var tasks = seatNumbers.Select(async seatNumber =>
            {
                var isLocked = await IsSeatLockedAsync(flightId, seatNumber);
                return new { SeatNumber = seatNumber, IsLocked = isLocked };
            });

            var results = await Task.WhenAll(tasks);
            
            foreach (var item in results)
            {
                result[item.SeatNumber] = item.IsLocked;
            }

            _logger.LogDebug("Retrieved lock status for {Count} seats on flight {FlightId}", result.Count, flightId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat lock status for flight {FlightId}", flightId);
        }

        return result;
    }

    public async Task<int> ReleaseSeatLocksForFlightAsync(Guid flightId)
    {
        try
        {
            var pattern = RedisKeyBuilder.BuildKey(_config.KeyPrefix, "locks", "seat", flightId.ToString(), "*");
            var deletedCount = await _redisService.DeleteByPatternAsync(pattern);
            
            _logger.LogInformation("Released {Count} seat locks for flight {FlightId}", deletedCount, flightId);
            return (int)deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing seat locks for flight {FlightId}", flightId);
            return 0;
        }
    }

    private string BuildSeatResource(Guid flightId, string seatNumber)
    {
        return $"seat:{flightId}:{seatNumber.ToUpperInvariant()}";
    }

    private async Task StoreSeatLockMetadataAsync(Guid flightId, string seatNumber, IRedLock redLock)
    {
        try
        {
            var metadataKey = RedisKeyBuilder.BuildKey(_config.KeyPrefix, "seat-metadata", flightId.ToString(), seatNumber);
            var metadata = new
            {
                FlightId = flightId,
                SeatNumber = seatNumber,
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(RedisTtl.MediumLock)
            };

            await _redisService.SetStringAsync(metadataKey, System.Text.Json.JsonSerializer.Serialize(metadata), RedisTtl.MediumLock);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store seat lock metadata for flight {FlightId}, seat {SeatNumber}", flightId, seatNumber);
            // Don't throw - metadata storage is not critical
        }
    }
}

/// <summary>
/// Specialized lock service for promotion redemption with contention tracking
/// </summary>
public class PromotionLockService : IPromotionLockService
{
    private readonly IDistributedLockService _lockService;
    private readonly IRedisService _redisService;
    private readonly IRedisMetricsService _metricsService;
    private readonly ILogger<PromotionLockService> _logger;
    private readonly RedisConfiguration _config;

    public PromotionLockService(
        IDistributedLockService lockService,
        IRedisService redisService,
        IRedisMetricsService metricsService,
        IOptions<RedisConfiguration> config,
        ILogger<PromotionLockService> logger)
    {
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IRedLock?> AcquirePromotionLockAsync(string promoCode, Guid? customerId = null, TimeSpan? timeout = null)
    {
        var resource = BuildPromotionResource(promoCode, customerId);
        var lockTimeout = timeout ?? RedisTtl.ShortLock;

        _logger.LogDebug("Attempting to acquire promotion lock for code {PromoCode}, customer {CustomerId}", promoCode, customerId);

        try
        {
            var redLock = await _lockService.AcquireLockAsync(resource, lockTimeout);
            
            if (redLock?.IsAcquired == true)
            {
                await RecordPromotionLockMetricsAsync(promoCode, true);
                _logger.LogDebug("Successfully acquired promotion lock for code {PromoCode}", promoCode);
            }
            else
            {
                await RecordPromotionLockMetricsAsync(promoCode, false);
                _logger.LogWarning("Failed to acquire promotion lock for code {PromoCode}", promoCode);
            }

            return redLock;
        }
        catch (Exception ex)
        {
            await RecordPromotionLockMetricsAsync(promoCode, false);
            _logger.LogError(ex, "Error acquiring promotion lock for code {PromoCode}", promoCode);
            throw;
        }
    }

    public async Task<bool> IsPromotionLockedAsync(string promoCode)
    {
        var resource = BuildPromotionResource(promoCode);
        
        try
        {
            return await _lockService.IsLockedAsync(resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking promotion lock status for code {PromoCode}", promoCode);
            return false;
        }
    }

    public async Task<T> ExecuteWithPromotionLockAsync<T>(string promoCode, Func<Task<T>> action, Guid? customerId = null)
    {
        var resource = BuildPromotionResource(promoCode, customerId);
        
        _logger.LogDebug("Executing action with promotion lock for code {PromoCode}", promoCode);

        try
        {
            return await _lockService.ExecuteWithLockAsync(resource, action, RedisTtl.ShortLock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action with promotion lock for code {PromoCode}", promoCode);
            throw;
        }
    }

    public async Task<LockContentionMetrics> GetPromotionContentionMetricsAsync(string promoCode)
    {
        try
        {
            var metricsKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "promotion-contention", promoCode);
            var metricsData = await _redisService.HashGetAllAsync(metricsKey);
            
            if (metricsData.Count == 0)
            {
                return new LockContentionMetrics { Resource = promoCode };
            }

            return new LockContentionMetrics
            {
                Resource = promoCode,
                TotalAttempts = int.Parse(metricsData.GetValueOrDefault("total_attempts", "0")),
                SuccessfulAcquisitions = int.Parse(metricsData.GetValueOrDefault("successful_acquisitions", "0")),
                FailedAcquisitions = int.Parse(metricsData.GetValueOrDefault("failed_acquisitions", "0")),
                TimeoutOccurrences = int.Parse(metricsData.GetValueOrDefault("timeout_occurrences", "0")),
                LastAttempt = DateTime.Parse(metricsData.GetValueOrDefault("last_attempt", DateTime.MinValue.ToString()))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotion contention metrics for code {PromoCode}", promoCode);
            return new LockContentionMetrics { Resource = promoCode };
        }
    }

    public async Task<int> CleanupPromotionLocksAsync()
    {
        try
        {
            var pattern = RedisKeyBuilder.BuildKey(_config.KeyPrefix, "locks", "promotion", "*");
            var deletedCount = await _redisService.DeleteByPatternAsync(pattern);
            
            _logger.LogInformation("Cleaned up {Count} promotion locks", deletedCount);
            return (int)deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up promotion locks");
            return 0;
        }
    }

    private string BuildPromotionResource(string promoCode, Guid? customerId = null)
    {
        var resource = $"promotion:{promoCode.ToUpperInvariant()}";
        if (customerId.HasValue)
        {
            resource += $":{customerId.Value}";
        }
        return resource;
    }

    private async Task RecordPromotionLockMetricsAsync(string promoCode, bool success)
    {
        try
        {
            var metricsKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "promotion-contention", promoCode);
            
            await _redisService.HashSetAsync(metricsKey, new Dictionary<string, string>
            {
                ["last_attempt"] = DateTime.UtcNow.ToString("O")
            });

            await _redisService.IncrementAsync($"{metricsKey}:total_attempts");
            
            if (success)
            {
                await _redisService.IncrementAsync($"{metricsKey}:successful_acquisitions");
            }
            else
            {
                await _redisService.IncrementAsync($"{metricsKey}:failed_acquisitions");
            }

            await _redisService.ExpireAsync(metricsKey, RedisTtl.Metrics);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record promotion lock metrics for code {PromoCode}", promoCode);
            // Don't throw - metrics recording is not critical
        }
    }
}
*/

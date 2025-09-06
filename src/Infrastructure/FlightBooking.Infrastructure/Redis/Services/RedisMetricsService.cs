using FlightBooking.Infrastructure.Redis.Configuration;
using FlightBooking.Infrastructure.Redis.Keys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace FlightBooking.Infrastructure.Redis.Services;

/// <summary>
/// Redis metrics service for tracking cache performance and lock contention
/// </summary>
public class RedisMetricsService : IRedisMetricsService
{
    private readonly IRedisService _redisService;
    private readonly ILogger<RedisMetricsService> _logger;
    private readonly RedisConfiguration _config;

    public RedisMetricsService(
        IRedisService redisService,
        IOptions<RedisConfiguration> config,
        ILogger<RedisMetricsService> logger)
    {
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Cache Metrics

    public async Task RecordCacheHitAsync(string category)
    {
        if (!_config.Metrics.EnableCacheMetrics) return;

        try
        {
            var hitKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "cache-hits", category);
            var totalKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "cache-total", category);

            await Task.WhenAll(
                _redisService.IncrementAsync(hitKey),
                _redisService.IncrementAsync(totalKey)
            );

            await Task.WhenAll(
                _redisService.ExpireAsync(hitKey, _config.Metrics.RetentionPeriod),
                _redisService.ExpireAsync(totalKey, _config.Metrics.RetentionPeriod)
            );

            _logger.LogDebug("Recorded cache hit for category {Category}", category);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record cache hit for category {Category}", category);
        }
    }

    public async Task RecordCacheMissAsync(string category)
    {
        if (!_config.Metrics.EnableCacheMetrics) return;

        try
        {
            var missKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "cache-misses", category);
            var totalKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "cache-total", category);

            await Task.WhenAll(
                _redisService.IncrementAsync(missKey),
                _redisService.IncrementAsync(totalKey)
            );

            await Task.WhenAll(
                _redisService.ExpireAsync(missKey, _config.Metrics.RetentionPeriod),
                _redisService.ExpireAsync(totalKey, _config.Metrics.RetentionPeriod)
            );

            _logger.LogDebug("Recorded cache miss for category {Category}", category);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record cache miss for category {Category}", category);
        }
    }

    public async Task<double> GetCacheHitRatioAsync(string category, TimeSpan? period = null)
    {
        try
        {
            var hitKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "cache-hits", category);
            var totalKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "cache-total", category);

            var hitCountTask = _redisService.GetStringAsync(hitKey);
            var totalCountTask = _redisService.GetStringAsync(totalKey);

            await Task.WhenAll(hitCountTask, totalCountTask);

            var hitCount = long.TryParse(hitCountTask.Result, out var hits) ? hits : 0;
            var totalCount = long.TryParse(totalCountTask.Result, out var total) ? total : 0;

            if (totalCount == 0) return 0.0;

            var hitRatio = (double)hitCount / totalCount;
            _logger.LogDebug("Cache hit ratio for category {Category}: {HitRatio:P2} ({Hits}/{Total})", 
                category, hitRatio, hitCount, totalCount);

            return hitRatio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cache hit ratio for category {Category}", category);
            return 0.0;
        }
    }

    public async Task<Dictionary<string, long>> GetCacheStatsAsync(TimeSpan? period = null)
    {
        try
        {
            var pattern = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "cache-*", "*");
            var keys = await _redisService.GetKeysByPatternAsync(pattern);

            var stats = new Dictionary<string, long>();

            foreach (var key in keys)
            {
                var value = await _redisService.GetStringAsync(key);
                if (long.TryParse(value, out var count))
                {
                    var keyParts = RedisKeyBuilder.ExtractParts(key);
                    if (keyParts.Length >= 3)
                    {
                        var metricName = $"{keyParts[2]}_{keyParts[3]}";
                        stats[metricName] = count;
                    }
                }
            }

            _logger.LogDebug("Retrieved cache stats: {StatsCount} metrics", stats.Count);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache stats");
            return new Dictionary<string, long>();
        }
    }

    #endregion

    #region Lock Metrics

    public async Task RecordLockAcquiredAsync(string resource, TimeSpan duration)
    {
        if (!_config.Metrics.EnableLockMetrics) return;

        try
        {
            var acquiredKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-acquired", resource);
            var durationKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-duration", resource);
            var totalKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-total", resource);

            await Task.WhenAll(
                _redisService.IncrementAsync(acquiredKey),
                _redisService.IncrementAsync(totalKey),
                RecordDurationMetricAsync(durationKey, duration)
            );

            await Task.WhenAll(
                _redisService.ExpireAsync(acquiredKey, _config.Metrics.RetentionPeriod),
                _redisService.ExpireAsync(durationKey, _config.Metrics.RetentionPeriod),
                _redisService.ExpireAsync(totalKey, _config.Metrics.RetentionPeriod)
            );

            _logger.LogDebug("Recorded lock acquisition for resource {Resource}, duration {Duration}ms", 
                resource, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record lock acquisition for resource {Resource}", resource);
        }
    }

    public async Task RecordLockContentionAsync(string resource)
    {
        if (!_config.Metrics.EnableLockMetrics) return;

        try
        {
            var contentionKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-contention", resource);
            var totalKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-total", resource);

            await Task.WhenAll(
                _redisService.IncrementAsync(contentionKey),
                _redisService.IncrementAsync(totalKey)
            );

            await Task.WhenAll(
                _redisService.ExpireAsync(contentionKey, _config.Metrics.RetentionPeriod),
                _redisService.ExpireAsync(totalKey, _config.Metrics.RetentionPeriod)
            );

            _logger.LogDebug("Recorded lock contention for resource {Resource}", resource);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record lock contention for resource {Resource}", resource);
        }
    }

    public async Task RecordLockTimeoutAsync(string resource)
    {
        if (!_config.Metrics.EnableLockMetrics) return;

        try
        {
            var timeoutKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-timeout", resource);
            var totalKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-total", resource);

            await Task.WhenAll(
                _redisService.IncrementAsync(timeoutKey),
                _redisService.IncrementAsync(totalKey)
            );

            await Task.WhenAll(
                _redisService.ExpireAsync(timeoutKey, _config.Metrics.RetentionPeriod),
                _redisService.ExpireAsync(totalKey, _config.Metrics.RetentionPeriod)
            );

            _logger.LogDebug("Recorded lock timeout for resource {Resource}", resource);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record lock timeout for resource {Resource}", resource);
        }
    }

    public async Task<Dictionary<string, object>> GetLockStatsAsync(string resource, TimeSpan? period = null)
    {
        try
        {
            var acquiredKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-acquired", resource);
            var contentionKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-contention", resource);
            var timeoutKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-timeout", resource);
            var totalKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "lock-total", resource);

            var tasks = new[]
            {
                _redisService.GetStringAsync(acquiredKey),
                _redisService.GetStringAsync(contentionKey),
                _redisService.GetStringAsync(timeoutKey),
                _redisService.GetStringAsync(totalKey)
            };

            await Task.WhenAll(tasks);

            var acquired = long.TryParse(tasks[0].Result, out var acq) ? acq : 0;
            var contention = long.TryParse(tasks[1].Result, out var cont) ? cont : 0;
            var timeout = long.TryParse(tasks[2].Result, out var time) ? time : 0;
            var total = long.TryParse(tasks[3].Result, out var tot) ? tot : 0;

            var stats = new Dictionary<string, object>
            {
                ["resource"] = resource,
                ["acquired"] = acquired,
                ["contention"] = contention,
                ["timeout"] = timeout,
                ["total"] = total,
                ["success_rate"] = total > 0 ? (double)acquired / total : 0.0,
                ["contention_rate"] = total > 0 ? (double)contention / total : 0.0,
                ["timeout_rate"] = total > 0 ? (double)timeout / total : 0.0
            };

            _logger.LogDebug("Retrieved lock stats for resource {Resource}: {Stats}", resource, stats);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lock stats for resource {Resource}", resource);
            return new Dictionary<string, object> { ["resource"] = resource };
        }
    }

    #endregion

    #region Performance Metrics

    public async Task RecordOperationDurationAsync(string operation, TimeSpan duration)
    {
        if (!_config.Metrics.EnablePerformanceMetrics) return;

        try
        {
            var durationKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "operation-duration", operation);
            var countKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "operation-count", operation);

            await Task.WhenAll(
                RecordDurationMetricAsync(durationKey, duration),
                _redisService.IncrementAsync(countKey)
            );

            await Task.WhenAll(
                _redisService.ExpireAsync(durationKey, _config.Metrics.RetentionPeriod),
                _redisService.ExpireAsync(countKey, _config.Metrics.RetentionPeriod)
            );

            _logger.LogDebug("Recorded operation duration for {Operation}: {Duration}ms", 
                operation, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record operation duration for {Operation}", operation);
        }
    }

    public async Task RecordErrorAsync(string operation, string errorType)
    {
        if (!_config.Metrics.EnablePerformanceMetrics) return;

        try
        {
            var errorKey = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "operation-error", $"{operation}:{errorType}");
            await _redisService.IncrementAsync(errorKey);
            await _redisService.ExpireAsync(errorKey, _config.Metrics.RetentionPeriod);

            _logger.LogDebug("Recorded error for operation {Operation}, type {ErrorType}", operation, errorType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record error for operation {Operation}", operation);
        }
    }

    public async Task<Dictionary<string, object>> GetPerformanceStatsAsync(TimeSpan? period = null)
    {
        try
        {
            var pattern = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "operation-*", "*");
            var keys = await _redisService.GetKeysByPatternAsync(pattern);

            var stats = new Dictionary<string, object>();

            foreach (var key in keys)
            {
                var value = await _redisService.GetStringAsync(key);
                if (!string.IsNullOrEmpty(value))
                {
                    var keyParts = RedisKeyBuilder.ExtractParts(key);
                    if (keyParts.Length >= 4)
                    {
                        var metricName = $"{keyParts[2]}_{keyParts[3]}";
                        stats[metricName] = value;
                    }
                }
            }

            _logger.LogDebug("Retrieved performance stats: {StatsCount} metrics", stats.Count);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance stats");
            return new Dictionary<string, object>();
        }
    }

    #endregion

    #region System Metrics

    public async Task<Dictionary<string, object>> GetSystemMetricsAsync()
    {
        try
        {
            var info = await _redisService.GetInfoAsync();
            var memoryUsage = await _redisService.GetMemoryUsageAsync();
            var dbSize = await _redisService.GetDatabaseSizeAsync();

            var metrics = new Dictionary<string, object>
            {
                ["database_size"] = dbSize,
                ["memory_usage"] = memoryUsage,
                ["connection_info"] = info.Where(kvp => kvp.Key.Contains("connected") || kvp.Key.Contains("client"))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["timestamp"] = DateTime.UtcNow
            };

            _logger.LogDebug("Retrieved system metrics");
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system metrics");
            return new Dictionary<string, object>();
        }
    }

    public async Task<Dictionary<string, long>> GetConnectionStatsAsync()
    {
        try
        {
            var info = await _redisService.GetInfoAsync();
            
            var stats = new Dictionary<string, long>();
            
            foreach (var kvp in info.Where(i => i.Key.Contains("connected") || i.Key.Contains("client")))
            {
                if (long.TryParse(kvp.Value, out var value))
                {
                    stats[kvp.Key] = value;
                }
            }

            _logger.LogDebug("Retrieved connection stats: {StatsCount} metrics", stats.Count);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection stats");
            return new Dictionary<string, long>();
        }
    }

    #endregion

    #region Cleanup

    public async Task CleanupOldMetricsAsync(TimeSpan olderThan)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - olderThan;
            var pattern = RedisKeyBuilder.MetricsKey(_config.KeyPrefix, "*", "*");
            var keys = await _redisService.GetKeysByPatternAsync(pattern);

            var deletedCount = 0;
            foreach (var key in keys)
            {
                var ttl = await _redisService.GetTtlAsync(key);
                if (ttl.HasValue && ttl.Value < TimeSpan.Zero)
                {
                    await _redisService.DeleteAsync(key);
                    deletedCount++;
                }
            }

            _logger.LogInformation("Cleaned up {Count} old metrics", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old metrics");
        }
    }

    #endregion

    #region Helper Methods

    private async Task RecordDurationMetricAsync(string key, TimeSpan duration)
    {
        // Store duration as milliseconds in a sorted set for percentile calculations
        var score = duration.TotalMilliseconds;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redisService.SortedSetAddAsync(key, timestamp.ToString(), score);
    }

    #endregion
}

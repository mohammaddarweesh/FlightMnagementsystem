using FlightBooking.Infrastructure.Redis.Configuration;
using FlightBooking.Infrastructure.Redis.Keys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System.Diagnostics;

namespace FlightBooking.Infrastructure.Redis.Services;

/// <summary>
/// Distributed lock service implementation using RedLock algorithm
/// TODO: Fix RedLock.net compatibility issues
/// </summary>
/*
public class DistributedLockService : IDistributedLockService, IDisposable
{
    private readonly IRedLockFactory _redLockFactory;
    private readonly IRedisMetricsService _metricsService;
    private readonly ILogger<DistributedLockService> _logger;
    private readonly RedisConfiguration _config;
    private readonly Random _random;
    private bool _disposed;

    public DistributedLockService(
        IConnectionMultiplexer connectionMultiplexer,
        IRedisMetricsService metricsService,
        IOptions<RedisConfiguration> config,
        ILogger<DistributedLockService> logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();

        // Configure RedLock with multiple Redis endpoints for high availability
        var redLockConfiguration = new RedLockConfiguration(new List<RedLockMultiplexer>
        {
            new(connectionMultiplexer)
        });

        _redLockFactory = RedLockFactory.Create(redLockConfiguration);
        
        _logger.LogInformation("Distributed lock service initialized");
    }

    public async Task<IRedLock?> AcquireLockAsync(string resource, string? lockId = null)
    {
        return await AcquireLockAsync(resource, _config.DistributedLock.DefaultTimeout, lockId);
    }

    public async Task<IRedLock?> AcquireLockAsync(string resource, TimeSpan timeout, string? lockId = null)
    {
        return await AcquireLockAsync(
            resource, 
            timeout, 
            _config.DistributedLock.DefaultRetryDelay, 
            _config.DistributedLock.MaxRetryAttempts, 
            lockId);
    }

    public async Task<IRedLock?> AcquireLockAsync(string resource, TimeSpan timeout, TimeSpan retryDelay, int maxRetryAttempts, string? lockId = null)
    {
        var lockKey = RedisKeyBuilder.LockKey(_config.KeyPrefix, "resource", resource);
        var actualLockId = lockId ?? Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        var attempts = 0;

        try
        {
            _logger.LogDebug("Attempting to acquire lock for resource {Resource} with timeout {Timeout}", resource, timeout);

            for (attempts = 0; attempts <= maxRetryAttempts; attempts++)
            {
                var redLock = await _redLockFactory.CreateLockAsync(lockKey, timeout);
                
                if (redLock.IsAcquired)
                {
                    stopwatch.Stop();
                    await _metricsService.RecordLockAcquiredAsync(resource, stopwatch.Elapsed);
                    
                    _logger.LogDebug("Successfully acquired lock for resource {Resource} after {Attempts} attempts in {Duration}ms", 
                        resource, attempts + 1, stopwatch.ElapsedMilliseconds);
                    
                    return redLock;
                }

                redLock.Dispose();

                if (attempts < maxRetryAttempts)
                {
                    var delay = CalculateRetryDelay(retryDelay, attempts);
                    await Task.Delay(delay);
                    await _metricsService.RecordLockContentionAsync(resource);
                }
            }

            stopwatch.Stop();
            await _metricsService.RecordLockTimeoutAsync(resource);
            
            _logger.LogWarning("Failed to acquire lock for resource {Resource} after {Attempts} attempts in {Duration}ms", 
                resource, attempts, stopwatch.ElapsedMilliseconds);
            
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error acquiring lock for resource {Resource} after {Attempts} attempts", resource, attempts);
            throw;
        }
    }

    public async Task<IRedLock?> TryAcquireLockAsync(string resource, TimeSpan timeout, string? lockId = null)
    {
        var lockKey = RedisKeyBuilder.LockKey(_config.KeyPrefix, "resource", resource);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var redLock = await _redLockFactory.CreateLockAsync(lockKey, timeout);
            stopwatch.Stop();

            if (redLock.IsAcquired)
            {
                await _metricsService.RecordLockAcquiredAsync(resource, stopwatch.Elapsed);
                _logger.LogDebug("Successfully acquired lock for resource {Resource} in {Duration}ms", resource, stopwatch.ElapsedMilliseconds);
                return redLock;
            }
            else
            {
                redLock.Dispose();
                await _metricsService.RecordLockContentionAsync(resource);
                _logger.LogDebug("Failed to acquire lock for resource {Resource} in {Duration}ms", resource, stopwatch.ElapsedMilliseconds);
                return null;
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error trying to acquire lock for resource {Resource}", resource);
            throw;
        }
    }

    public async Task<T> ExecuteWithLockAsync<T>(string resource, Func<Task<T>> action, TimeSpan? timeout = null)
    {
        var lockTimeout = timeout ?? _config.DistributedLock.DefaultTimeout;
        
        using var redLock = await AcquireLockAsync(resource, lockTimeout);
        
        if (redLock == null || !redLock.IsAcquired)
        {
            throw new InvalidOperationException($"Failed to acquire lock for resource '{resource}' within timeout {lockTimeout}");
        }

        try
        {
            _logger.LogDebug("Executing action with lock for resource {Resource}", resource);
            return await action();
        }
        finally
        {
            _logger.LogDebug("Completed action with lock for resource {Resource}", resource);
        }
    }

    public async Task ExecuteWithLockAsync(string resource, Func<Task> action, TimeSpan? timeout = null)
    {
        await ExecuteWithLockAsync<object>(resource, async () =>
        {
            await action();
            return null!;
        }, timeout);
    }

    public async Task<T> ExecuteWithLockAsync<T>(string resource, Func<Task<T>> action, TimeSpan timeout, TimeSpan retryDelay, int maxRetryAttempts)
    {
        using var redLock = await AcquireLockAsync(resource, timeout, retryDelay, maxRetryAttempts);
        
        if (redLock == null || !redLock.IsAcquired)
        {
            throw new InvalidOperationException($"Failed to acquire lock for resource '{resource}' after {maxRetryAttempts} attempts");
        }

        try
        {
            _logger.LogDebug("Executing action with lock for resource {Resource}", resource);
            return await action();
        }
        finally
        {
            _logger.LogDebug("Completed action with lock for resource {Resource}", resource);
        }
    }

    public async Task<bool> IsLockedAsync(string resource)
    {
        try
        {
            var lockKey = RedisKeyBuilder.LockKey(_config.KeyPrefix, "resource", resource);
            using var testLock = await _redLockFactory.CreateLockAsync(lockKey, TimeSpan.FromMilliseconds(1));
            return !testLock.IsAcquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if resource {Resource} is locked", resource);
            return false;
        }
    }

    public async Task<LockInfo?> GetLockInfoAsync(string resource)
    {
        // This is a simplified implementation
        // In a real scenario, you'd store lock metadata in Redis
        try
        {
            var isLocked = await IsLockedAsync(resource);
            if (!isLocked) return null;

            return new LockInfo
            {
                Resource = resource,
                LockId = "unknown", // Would need to be stored separately
                AcquiredAt = DateTime.UtcNow, // Would need to be stored separately
                ExpiresAt = DateTime.UtcNow.Add(_config.DistributedLock.DefaultTimeout), // Estimated
                Owner = "unknown" // Would need to be stored separately
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lock info for resource {Resource}", resource);
            return null;
        }
    }

    public async Task<bool> ForceReleaseLockAsync(string resource)
    {
        try
        {
            var lockKey = RedisKeyBuilder.LockKey(_config.KeyPrefix, "resource", resource);
            // This would require access to the underlying Redis connection
            // Implementation depends on specific RedLock library capabilities
            _logger.LogWarning("Force release requested for resource {Resource}", resource);
            return false; // Not implemented in this simplified version
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error force releasing lock for resource {Resource}", resource);
            return false;
        }
    }

    public async Task<IEnumerable<LockInfo>> GetActiveLocksAsync()
    {
        try
        {
            // This would require scanning Redis for lock keys
            // Implementation depends on specific requirements
            _logger.LogDebug("Getting active locks");
            return new List<LockInfo>(); // Simplified implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active locks");
            return new List<LockInfo>();
        }
    }

    public async Task<int> CleanupExpiredLocksAsync()
    {
        try
        {
            // RedLock automatically handles expiration, but we might want to clean up metadata
            _logger.LogDebug("Cleaning up expired locks");
            return 0; // Simplified implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired locks");
            return 0;
        }
    }

    private TimeSpan CalculateRetryDelay(TimeSpan baseDelay, int attempt)
    {
        if (!_config.DistributedLock.EnableJitter)
            return baseDelay;

        // Add jitter to prevent thundering herd
        var jitterFactor = _config.DistributedLock.JitterFactor;
        var jitter = _random.NextDouble() * jitterFactor;
        var multiplier = 1.0 + jitter;
        
        // Exponential backoff with jitter
        var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt) * multiplier);
        
        // Cap the delay to prevent excessive waiting
        var maxDelay = TimeSpan.FromSeconds(30);
        return delay > maxDelay ? maxDelay : delay;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _redLockFactory?.Dispose();
            _disposed = true;
            _logger.LogInformation("Distributed lock service disposed");
        }
    }
}
*/

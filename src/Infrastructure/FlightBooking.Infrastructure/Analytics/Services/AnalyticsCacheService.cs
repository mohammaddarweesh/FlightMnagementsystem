using FlightBooking.Application.Analytics.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlightBooking.Infrastructure.Analytics.Services;

/// <summary>
/// Cache service for analytics data using distributed cache
/// </summary>
public class AnalyticsCacheService : IAnalyticsCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AnalyticsCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AnalyticsCacheService(
        IDistributedCache distributedCache,
        ILogger<AnalyticsCacheService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cached value for key: {CacheKey}", key);
            return null; // Return null on cache errors to allow fallback to data source
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            
            _logger.LogDebug("Cached value for key: {CacheKey}, Expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache value for key: {CacheKey}", key);
            // Don't throw here as caching is not critical for functionality
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cached value for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cached value for key: {CacheKey}", key);
            // Don't throw here as cache removal failures are not critical
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        try
        {
            // Note: This is a simplified implementation
            // In a real Redis implementation, you would use SCAN with pattern matching
            // For now, we'll log that pattern-based removal was requested
            _logger.LogInformation("Pattern-based cache removal requested for pattern: {Pattern}", pattern);
            
            // This would need to be implemented based on the specific cache provider
            // For Redis: use SCAN command with pattern
            // For SQL Server cache: query the cache table
            // For in-memory cache: iterate through keys
            
            _logger.LogWarning("Pattern-based cache removal not fully implemented for pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cached values by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            var exists = !string.IsNullOrEmpty(cachedValue);
            
            _logger.LogDebug("Cache key existence check for {CacheKey}: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check cache key existence for: {CacheKey}", key);
            return false;
        }
    }

    public string GenerateKey(string prefix, params object[] parameters)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));

        var keyParts = new List<string> { "analytics", prefix };
        
        foreach (var param in parameters)
        {
            if (param != null)
            {
                var paramString = param switch
                {
                    DateTime dt => dt.ToString("yyyyMMdd"),
                    DateOnly date => date.ToString("yyyyMMdd"),
                    IEnumerable<string> strings => string.Join("-", strings.OrderBy(s => s)),
                    _ => param.ToString()
                };
                
                if (!string.IsNullOrEmpty(paramString))
                {
                    keyParts.Add(paramString);
                }
            }
        }

        var key = string.Join(":", keyParts);
        
        // Ensure key length is reasonable (Redis has a 512MB limit, but shorter is better)
        if (key.Length > 250)
        {
            // Hash long keys to keep them manageable
            var hash = key.GetHashCode().ToString("X");
            key = $"{prefix}:hash:{hash}";
        }

        _logger.LogDebug("Generated cache key: {CacheKey}", key);
        return key;
    }
}

/// <summary>
/// In-memory cache service for analytics data (fallback implementation)
/// </summary>
public class InMemoryAnalyticsCacheService : IAnalyticsCacheService
{
    private readonly Dictionary<string, (object Value, DateTime Expiration)> _cache = new();
    private readonly object _lockObject = new();
    private readonly ILogger<InMemoryAnalyticsCacheService> _logger;

    public InMemoryAnalyticsCacheService(ILogger<InMemoryAnalyticsCacheService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        lock (_lockObject)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                if (cached.Expiration > DateTime.UtcNow)
                {
                    _logger.LogDebug("In-memory cache hit for key: {CacheKey}", key);
                    return Task.FromResult(cached.Value as T);
                }
                else
                {
                    // Remove expired entry
                    _cache.Remove(key);
                    _logger.LogDebug("Removed expired cache entry for key: {CacheKey}", key);
                }
            }

            _logger.LogDebug("In-memory cache miss for key: {CacheKey}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        lock (_lockObject)
        {
            var expirationTime = DateTime.UtcNow.Add(expiration);
            _cache[key] = (value, expirationTime);
            
            _logger.LogDebug("Cached value in memory for key: {CacheKey}, Expiration: {Expiration}", key, expirationTime);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        lock (_lockObject)
        {
            var removed = _cache.Remove(key);
            if (removed)
            {
                _logger.LogDebug("Removed cached value from memory for key: {CacheKey}", key);
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        lock (_lockObject)
        {
            var keysToRemove = _cache.Keys
                .Where(key => key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            _logger.LogDebug("Removed {Count} cached values from memory matching pattern: {Pattern}", 
                keysToRemove.Count, pattern);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        lock (_lockObject)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                if (cached.Expiration > DateTime.UtcNow)
                {
                    return Task.FromResult(true);
                }
                else
                {
                    // Remove expired entry
                    _cache.Remove(key);
                }
            }

            return Task.FromResult(false);
        }
    }

    public string GenerateKey(string prefix, params object[] parameters)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));

        var keyParts = new List<string> { "analytics", prefix };
        
        foreach (var param in parameters)
        {
            if (param != null)
            {
                var paramString = param switch
                {
                    DateTime dt => dt.ToString("yyyyMMdd"),
                    DateOnly date => date.ToString("yyyyMMdd"),
                    IEnumerable<string> strings => string.Join("-", strings.OrderBy(s => s)),
                    _ => param.ToString()
                };
                
                if (!string.IsNullOrEmpty(paramString))
                {
                    keyParts.Add(paramString);
                }
            }
        }

        var key = string.Join(":", keyParts);
        _logger.LogDebug("Generated in-memory cache key: {CacheKey}", key);
        return key;
    }

    // Cleanup method to remove expired entries
    public void CleanupExpiredEntries()
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.Expiration <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }

            if (expiredKeys.Any())
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }
        }
    }
}

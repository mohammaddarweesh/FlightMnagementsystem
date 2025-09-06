using FlightBooking.Infrastructure.Redis.Configuration;
using FlightBooking.Infrastructure.Redis.Keys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlightBooking.Infrastructure.Redis.Services;

/// <summary>
/// Redis cache service with typed operations and metrics tracking
/// </summary>
public class RedisCacheService : IRedisCacheService
{
    private readonly IRedisService _redisService;
    private readonly IRedisMetricsService _metricsService;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly RedisConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IRedisService redisService,
        IRedisMetricsService metricsService,
        IOptions<RedisConfiguration> config,
        ILogger<RedisCacheService> logger)
    {
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key))
            return null;

        var category = GetCategoryFromKey(key);

        try
        {
            var cachedValue = await _redisService.GetStringAsync(key);
            
            if (cachedValue == null)
            {
                await _metricsService.RecordCacheMissAsync(category);
                _logger.LogDebug("Cache miss for key {Key}", key);
                return null;
            }

            var deserializedValue = JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
            await _metricsService.RecordCacheHitAsync(category);
            
            _logger.LogDebug("Cache hit for key {Key}", key);
            return deserializedValue;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached value for key {Key}", key);
            await _redisService.DeleteAsync(key); // Remove corrupted data
            await _metricsService.RecordCacheMissAsync(category);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key {Key}", key);
            await _metricsService.RecordCacheMissAsync(category);
            return null;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        if (string.IsNullOrEmpty(key) || value == null)
            return false;

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var cacheExpiry = expiry ?? _config.DefaultTtl;
            
            var success = await _redisService.SetStringAsync(key, serializedValue, cacheExpiry);
            
            if (success)
            {
                _logger.LogDebug("Cached value for key {Key} with expiry {Expiry}", key, cacheExpiry);
            }
            else
            {
                _logger.LogWarning("Failed to cache value for key {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching value for key {Key}", key);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        try
        {
            var success = await _redisService.DeleteAsync(key);
            
            if (success)
            {
                _logger.LogDebug("Deleted cached value for key {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cached value for key {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        try
        {
            return await _redisService.ExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of key {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
    {
        if (string.IsNullOrEmpty(key) || factory == null)
            throw new ArgumentException("Key and factory cannot be null or empty");

        var cachedValue = await GetAsync<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        try
        {
            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiry);
            }
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSet factory for key {Key}", key);
            throw;
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(params string[] keys) where T : class
    {
        if (keys == null || keys.Length == 0)
            return new Dictionary<string, T?>();

        var result = new Dictionary<string, T?>();

        try
        {
            var values = await _redisService.GetManyAsync(keys);
            
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var value = values[i];
                var category = GetCategoryFromKey(key);

                if (value == null)
                {
                    result[key] = null;
                    await _metricsService.RecordCacheMissAsync(category);
                }
                else
                {
                    try
                    {
                        var deserializedValue = JsonSerializer.Deserialize<T>(value, _jsonOptions);
                        result[key] = deserializedValue;
                        await _metricsService.RecordCacheHitAsync(category);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize cached value for key {Key}", key);
                        result[key] = null;
                        await _redisService.DeleteAsync(key); // Remove corrupted data
                        await _metricsService.RecordCacheMissAsync(category);
                    }
                }
            }

            _logger.LogDebug("Retrieved {Count} cached values", keys.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple cached values");
            // Return empty results for all keys on error
            foreach (var key in keys)
            {
                result[key] = null;
                var category = GetCategoryFromKey(key);
                await _metricsService.RecordCacheMissAsync(category);
            }
        }

        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValues, TimeSpan? expiry = null) where T : class
    {
        if (keyValues == null || keyValues.Count == 0)
            return;

        try
        {
            var serializedKeyValues = new Dictionary<string, string>();
            
            foreach (var kvp in keyValues)
            {
                if (kvp.Value != null)
                {
                    serializedKeyValues[kvp.Key] = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
                }
            }

            await _redisService.SetManyAsync(serializedKeyValues, expiry ?? _config.DefaultTtl);
            
            _logger.LogDebug("Cached {Count} values", serializedKeyValues.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching multiple values");
            throw;
        }
    }

    public async Task<long> DeleteByPatternAsync(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return 0;

        try
        {
            var deletedCount = await _redisService.DeleteByPatternAsync(pattern);
            
            _logger.LogDebug("Deleted {Count} cached values matching pattern {Pattern}", deletedCount, pattern);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cached values by pattern {Pattern}", pattern);
            return 0;
        }
    }

    public async Task InvalidateTagAsync(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return;

        try
        {
            // Use a pattern to find all keys with the tag
            var pattern = RedisKeyBuilder.BuildKey(_config.KeyPrefix, "cache", "*", $"*{tag}*");
            await DeleteByPatternAsync(pattern);
            
            _logger.LogDebug("Invalidated cache entries with tag {Tag}", tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache tag {Tag}", tag);
        }
    }

    public async Task<bool> RefreshAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        try
        {
            var exists = await _redisService.ExistsAsync(key);
            if (!exists)
                return false;

            // Extend the TTL by the default TTL
            var success = await _redisService.ExpireAsync(key, _config.DefaultTtl);
            
            if (success)
            {
                _logger.LogDebug("Refreshed TTL for key {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing key {Key}", key);
            return false;
        }
    }

    private string GetCategoryFromKey(string key)
    {
        try
        {
            var category = RedisKeyBuilder.GetCategory(key);
            return category ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}

/// <summary>
/// Cache helper extensions for common caching patterns
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// Build a cache key for booking data
    /// </summary>
    public static string BookingCacheKey(this IRedisCacheService cache, Guid bookingId)
        => RedisKeyBuilder.CacheKey("flightbooking", "booking", bookingId.ToString());

    /// <summary>
    /// Build a cache key for flight availability
    /// </summary>
    public static string FlightAvailabilityCacheKey(this IRedisCacheService cache, string route, DateTime date)
        => RedisKeyBuilder.FlightAvailabilityKey("flightbooking", route, date);

    /// <summary>
    /// Build a cache key for pricing data
    /// </summary>
    public static string PricingCacheKey(this IRedisCacheService cache, Guid flightId, string fareClass)
        => RedisKeyBuilder.PricingKey("flightbooking", flightId, fareClass);

    /// <summary>
    /// Build a cache key for user data
    /// </summary>
    public static string UserCacheKey(this IRedisCacheService cache, Guid userId)
        => RedisKeyBuilder.CacheKey("flightbooking", "user", userId.ToString());

    /// <summary>
    /// Build a cache key for promotion data
    /// </summary>
    public static string PromotionCacheKey(this IRedisCacheService cache, string promoCode)
        => RedisKeyBuilder.PromotionKey("flightbooking", promoCode);
}

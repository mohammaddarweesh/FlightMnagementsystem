using FlightBooking.Application.Search.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace FlightBooking.Infrastructure.Search.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _keyPrefix;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
        _keyPrefix = "flightbooking:";
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullKey = GetFullKey(key);
            var value = await _database.StringGetAsync(fullKey);
            
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullKey = GetFullKey(key);
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            await _database.StringSetAsync(fullKey, serializedValue, expiry);
            _logger.LogDebug("Cached value for key: {Key} with expiry: {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await factory();
        await SetAsync(key, value, expiry, cancellationToken);
        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            await _database.KeyDeleteAsync(fullKey);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPattern = GetFullKey(pattern);
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: fullPattern).ToArray();
            
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            return await _database.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            return await _database.KeyTimeToLiveAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for cache key: {Key}", key);
            return null;
        }
    }

    public async Task ExtendTtlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            await _database.KeyExpireAsync(fullKey, expiry);
            _logger.LogDebug("Extended TTL for cache key: {Key} to {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending TTL for cache key: {Key}", key);
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var info = await server.InfoAsync("stats");
            
            var stats = new CacheStatistics
            {
                LastReset = DateTime.UtcNow
            };

            // Parse Redis INFO stats
            foreach (var line in info.ToString().Split('\n'))
            {
                if (line.StartsWith("keyspace_hits:"))
                    stats.HitCount = long.Parse(line.Split(':')[1]);
                else if (line.StartsWith("keyspace_misses:"))
                    stats.MissCount = long.Parse(line.Split(':')[1]);
                else if (line.StartsWith("used_memory:"))
                    stats.MemoryUsage = long.Parse(line.Split(':')[1]);
            }

            // Count keys with our prefix
            var keys = server.Keys(pattern: $"{_keyPrefix}*");
            stats.TotalKeys = keys.Count();

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics { LastReset = DateTime.UtcNow };
        }
    }

    public async Task InvalidateByTagsAsync(params string[] tags)
    {
        try
        {
            var tasks = new List<Task>();
            
            foreach (var tag in tags)
            {
                var tagKey = GetTagKey(tag);
                var taggedKeys = await _database.SetMembersAsync(tagKey);
                
                if (taggedKeys.Length > 0)
                {
                    // Remove all keys associated with this tag
                    tasks.Add(_database.KeyDeleteAsync(taggedKeys.Select(k => (RedisKey)k.ToString()).ToArray()));
                    // Remove the tag set itself
                    tasks.Add(_database.KeyDeleteAsync(tagKey));
                }
            }

            await Task.WhenAll(tasks);
            _logger.LogDebug("Invalidated cache by tags: {Tags}", string.Join(", ", tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by tags: {Tags}", string.Join(", ", tags));
        }
    }

    public async Task SetWithTagsAsync<T>(string key, T value, TimeSpan? expiry = null, params string[] tags) where T : class
    {
        try
        {
            // Set the main cache entry
            await SetAsync(key, value, expiry);

            // Associate the key with tags
            var fullKey = GetFullKey(key);
            var tasks = new List<Task>();

            foreach (var tag in tags)
            {
                var tagKey = GetTagKey(tag);
                tasks.Add(_database.SetAddAsync(tagKey, fullKey));
                
                // Set expiry on tag set (slightly longer than cache entry)
                if (expiry.HasValue)
                {
                    tasks.Add(_database.KeyExpireAsync(tagKey, expiry.Value.Add(TimeSpan.FromMinutes(5))));
                }
            }

            await Task.WhenAll(tasks);
            _logger.LogDebug("Cached value for key: {Key} with tags: {Tags}", key, string.Join(", ", tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value with tags for key: {Key}", key);
        }
    }

    private string GetFullKey(string key) => $"{_keyPrefix}{key}";
    private string GetTagKey(string tag) => $"{_keyPrefix}tags:{tag}";
}

public class FlightSearchCacheService : IFlightSearchCacheService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<FlightSearchCacheService> _logger;
    private readonly TimeSpan _searchResultsTtl = TimeSpan.FromMinutes(3);
    private readonly TimeSpan _availabilityTtl = TimeSpan.FromMinutes(2);

    public FlightSearchCacheService(ICacheService cacheService, ILogger<FlightSearchCacheService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<CachedSearchResult?> GetSearchResultsAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _cacheService.GetAsync<CachedSearchResult>(cacheKey, cancellationToken);
            if (result != null)
            {
                _logger.LogDebug("Search cache hit for key: {CacheKey}", cacheKey);
                return result;
            }

            _logger.LogDebug("Search cache miss for key: {CacheKey}", cacheKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached search results for key: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task SetSearchResultsAsync(string cacheKey, CachedSearchResult results, CancellationToken cancellationToken = default)
    {
        try
        {
            results.CachedAt = DateTime.UtcNow;
            results.ExpiresAt = DateTime.UtcNow.Add(_searchResultsTtl);

            await _cacheService.SetWithTagsAsync(cacheKey, results, _searchResultsTtl, results.Tags.ToArray());
            _logger.LogDebug("Cached search results for key: {CacheKey} with {TagCount} tags", cacheKey, results.Tags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching search results for key: {CacheKey}", cacheKey);
        }
    }

    public async Task<CachedAvailability?> GetAvailabilityAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _cacheService.GetAsync<CachedAvailability>(cacheKey, cancellationToken);
            if (result != null)
            {
                _logger.LogDebug("Availability cache hit for key: {CacheKey}", cacheKey);
                return result;
            }

            _logger.LogDebug("Availability cache miss for key: {CacheKey}", cacheKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached availability for key: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task SetAvailabilityAsync(string cacheKey, CachedAvailability availability, CancellationToken cancellationToken = default)
    {
        try
        {
            availability.CachedAt = DateTime.UtcNow;
            availability.ExpiresAt = DateTime.UtcNow.Add(_availabilityTtl);

            await _cacheService.SetWithTagsAsync(cacheKey, availability, _availabilityTtl, availability.Tags.ToArray());
            _logger.LogDebug("Cached availability for key: {CacheKey} with {TagCount} tags", cacheKey, availability.Tags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching availability for key: {CacheKey}", cacheKey);
        }
    }

    public async Task InvalidateRouteAsync(string departureAirport, string arrivalAirport, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            var routeTag = $"route:{departureAirport}-{arrivalAirport}";
            var dateTag = $"date:{date:yyyy-MM-dd}";

            await _cacheService.InvalidateByTagsAsync(routeTag, dateTag);
            _logger.LogInformation("Invalidated cache for route {Route} on {Date}", $"{departureAirport}-{arrivalAirport}", date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating route cache for {Route} on {Date}", $"{departureAirport}-{arrivalAirport}", date);
        }
    }

    public async Task InvalidateFlightAsync(Guid flightId, CancellationToken cancellationToken = default)
    {
        try
        {
            var flightTag = $"flight:{flightId}";
            await _cacheService.InvalidateByTagsAsync(flightTag);
            _logger.LogInformation("Invalidated cache for flight {FlightId}", flightId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating flight cache for {FlightId}", flightId);
        }
    }

    public async Task InvalidateFareClassAsync(Guid fareClassId, CancellationToken cancellationToken = default)
    {
        try
        {
            var fareClassTag = $"fareclass:{fareClassId}";
            await _cacheService.InvalidateByTagsAsync(fareClassTag);
            _logger.LogInformation("Invalidated cache for fare class {FareClassId}", fareClassId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating fare class cache for {FareClassId}", fareClassId);
        }
    }

    public async Task InvalidateAllSearchCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.RemoveByPatternAsync("flight_search:*", cancellationToken);
            await _cacheService.RemoveByPatternAsync("availability:*", cancellationToken);
            _logger.LogInformation("Invalidated all search cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all search cache");
        }
    }

    public async Task<SearchCacheStatistics> GetSearchCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var baseStats = await _cacheService.GetStatisticsAsync(cancellationToken);

            // TODO: Implement specific search cache statistics tracking
            // This would require additional Redis data structures to track search-specific metrics

            return new SearchCacheStatistics
            {
                TotalKeys = baseStats.TotalKeys,
                HitCount = baseStats.HitCount,
                MissCount = baseStats.MissCount,
                MemoryUsage = baseStats.MemoryUsage,
                LastReset = baseStats.LastReset,
                // TODO: Implement search-specific metrics
                SearchHits = 0,
                SearchMisses = 0,
                AvailabilityHits = 0,
                AvailabilityMisses = 0,
                MostCachedRoutes = new List<string>(),
                MostInvalidatedTags = new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search cache statistics");
            return new SearchCacheStatistics { LastReset = DateTime.UtcNow };
        }
    }
}

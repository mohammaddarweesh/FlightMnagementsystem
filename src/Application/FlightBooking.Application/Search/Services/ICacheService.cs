namespace FlightBooking.Application.Search.Services;

public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a cached value with TTL
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets or sets a cached value using cache-aside pattern
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a cached value by key
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple cached values by pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the TTL for a cached key
    /// </summary>
    Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extends the TTL for a cached key
    /// </summary>
    Task ExtendTtlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries by tags
    /// </summary>
    Task InvalidateByTagsAsync(params string[] tags);

    /// <summary>
    /// Sets a cached value with tags for invalidation
    /// </summary>
    Task SetWithTagsAsync<T>(string key, T value, TimeSpan? expiry = null, params string[] tags) where T : class;
}

public interface IFlightSearchCacheService
{
    /// <summary>
    /// Gets cached flight search results
    /// </summary>
    Task<CachedSearchResult?> GetSearchResultsAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches flight search results with appropriate TTL and tags
    /// </summary>
    Task SetSearchResultsAsync(string cacheKey, CachedSearchResult results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached flight availability data
    /// </summary>
    Task<CachedAvailability?> GetAvailabilityAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches flight availability data
    /// </summary>
    Task SetAvailabilityAsync(string cacheKey, CachedAvailability availability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates search cache for specific route and date
    /// </summary>
    Task InvalidateRouteAsync(string departureAirport, string arrivalAirport, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates search cache for specific flight
    /// </summary>
    Task InvalidateFlightAsync(Guid flightId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates search cache for specific fare class
    /// </summary>
    Task InvalidateFareClassAsync(Guid fareClassId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all search cache
    /// </summary>
    Task InvalidateAllSearchCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache hit/miss statistics for monitoring
    /// </summary>
    Task<SearchCacheStatistics> GetSearchCacheStatisticsAsync(CancellationToken cancellationToken = default);
}

public class CachedSearchResult
{
    public string Data { get; set; } = string.Empty; // JSON serialized FlightSearchResult
    public string ETag { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class CachedAvailability
{
    public string Data { get; set; } = string.Empty; // JSON serialized availability data
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class CacheStatistics
{
    public long TotalKeys { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
    public long TotalRequests => HitCount + MissCount;
    public long MemoryUsage { get; set; }
    public DateTime LastReset { get; set; }
}

public class SearchCacheStatistics : CacheStatistics
{
    public long SearchHits { get; set; }
    public long SearchMisses { get; set; }
    public long AvailabilityHits { get; set; }
    public long AvailabilityMisses { get; set; }
    public double SearchHitRatio => (SearchHits + SearchMisses) > 0 ? (double)SearchHits / (SearchHits + SearchMisses) : 0;
    public double AvailabilityHitRatio => (AvailabilityHits + AvailabilityMisses) > 0 ? (double)AvailabilityHits / (AvailabilityHits + AvailabilityMisses) : 0;
    public List<string> MostCachedRoutes { get; set; } = new();
    public List<string> MostInvalidatedTags { get; set; } = new();
}

using StackExchange.Redis;

namespace FlightBooking.Infrastructure.Redis.Services;

/// <summary>
/// Redis service interface for common operations
/// </summary>
public interface IRedisService
{
    /// <summary>
    /// Get the Redis database
    /// </summary>
    IDatabase Database { get; }

    /// <summary>
    /// Get the Redis connection multiplexer
    /// </summary>
    IConnectionMultiplexer Connection { get; }

    /// <summary>
    /// Check if Redis is connected
    /// </summary>
    bool IsConnected { get; }

    // String operations
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<string?> GetStringAsync(string key);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<bool> ExpireAsync(string key, TimeSpan expiry);
    Task<TimeSpan?> GetTtlAsync(string key);

    // Hash operations
    Task<bool> HashSetAsync(string key, string field, string value);
    Task<bool> HashSetAsync(string key, Dictionary<string, string> values);
    Task<string?> HashGetAsync(string key, string field);
    Task<Dictionary<string, string>> HashGetAllAsync(string key);
    Task<bool> HashDeleteAsync(string key, string field);
    Task<bool> HashExistsAsync(string key, string field);

    // Set operations
    Task<bool> SetAddAsync(string key, string value);
    Task<bool> SetRemoveAsync(string key, string value);
    Task<bool> SetContainsAsync(string key, string value);
    Task<string[]> SetMembersAsync(string key);
    Task<long> SetLengthAsync(string key);

    // List operations
    Task<long> ListPushAsync(string key, string value);
    Task<string?> ListPopAsync(string key);
    Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1);
    Task<long> ListLengthAsync(string key);

    // Sorted set operations
    Task<bool> SortedSetAddAsync(string key, string member, double score);
    Task<string[]> SortedSetRangeByScoreAsync(string key, double min, double max);
    Task<bool> SortedSetRemoveAsync(string key, string member);
    Task<long> SortedSetLengthAsync(string key);

    // Atomic operations
    Task<long> IncrementAsync(string key, long value = 1);
    Task<long> DecrementAsync(string key, long value = 1);
    Task<double> IncrementAsync(string key, double value);

    // Batch operations
    Task<bool[]> DeleteManyAsync(params string[] keys);
    Task<string?[]> GetManyAsync(params string[] keys);
    Task SetManyAsync(Dictionary<string, string> keyValues, TimeSpan? expiry = null);

    // Pattern operations
    Task<string[]> GetKeysByPatternAsync(string pattern);
    Task<long> DeleteByPatternAsync(string pattern);

    // Pub/Sub operations
    Task PublishAsync(string channel, string message);
    Task SubscribeAsync(string channel, Action<string, string> handler);
    Task UnsubscribeAsync(string channel);

    // Transaction operations
    Task<bool> ExecuteTransactionAsync(Func<ITransaction, Task> operations);

    // Lua script operations
    Task<RedisResult> ExecuteScriptAsync(string script, string[] keys, RedisValue[] values);

    // Health check
    Task<bool> PingAsync();
    Task<Dictionary<string, string>> GetInfoAsync();

    // Metrics
    Task<long> GetDatabaseSizeAsync();
    Task<Dictionary<string, long>> GetMemoryUsageAsync();
}

/// <summary>
/// Redis cache service interface for typed operations
/// </summary>
public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class;
    Task<Dictionary<string, T?>> GetManyAsync<T>(params string[] keys) where T : class;
    Task SetManyAsync<T>(Dictionary<string, T> keyValues, TimeSpan? expiry = null) where T : class;
    Task<long> DeleteByPatternAsync(string pattern);
    Task InvalidateTagAsync(string tag);
    Task<bool> RefreshAsync(string key);
}

/// <summary>
/// Redis session service interface
/// </summary>
public interface IRedisSessionService
{
    Task<string> CreateSessionAsync(bool isAuthenticated = false, Guid? userId = null);
    Task<bool> ValidateSessionAsync(string sessionId);
    Task<T?> GetSessionDataAsync<T>(string sessionId, string key) where T : class;
    Task<bool> SetSessionDataAsync<T>(string sessionId, string key, T value) where T : class;
    Task<bool> RemoveSessionDataAsync(string sessionId, string key);
    Task<bool> RefreshSessionAsync(string sessionId);
    Task<bool> DestroySessionAsync(string sessionId);
    Task<Dictionary<string, object>> GetAllSessionDataAsync(string sessionId);
    Task<bool> IsSessionExpiredAsync(string sessionId);
    Task<TimeSpan?> GetSessionTtlAsync(string sessionId);
    Task<long> CleanupExpiredSessionsAsync();
}

/// <summary>
/// Redis metrics service interface
/// </summary>
public interface IRedisMetricsService
{
    // Cache metrics
    Task RecordCacheHitAsync(string category);
    Task RecordCacheMissAsync(string category);
    Task<double> GetCacheHitRatioAsync(string category, TimeSpan? period = null);
    Task<Dictionary<string, long>> GetCacheStatsAsync(TimeSpan? period = null);

    // Lock metrics
    Task RecordLockAcquiredAsync(string resource, TimeSpan duration);
    Task RecordLockContentionAsync(string resource);
    Task RecordLockTimeoutAsync(string resource);
    Task<Dictionary<string, object>> GetLockStatsAsync(string resource, TimeSpan? period = null);

    // Performance metrics
    Task RecordOperationDurationAsync(string operation, TimeSpan duration);
    Task RecordErrorAsync(string operation, string errorType);
    Task<Dictionary<string, object>> GetPerformanceStatsAsync(TimeSpan? period = null);

    // System metrics
    Task<Dictionary<string, object>> GetSystemMetricsAsync();
    Task<Dictionary<string, long>> GetConnectionStatsAsync();

    // Cleanup
    Task CleanupOldMetricsAsync(TimeSpan olderThan);
}

/// <summary>
/// Redis health check service interface
/// </summary>
public interface IRedisHealthService
{
    Task<bool> IsHealthyAsync();
    Task<Dictionary<string, object>> GetHealthDetailsAsync();
    Task<bool> CanConnectAsync();
    Task<TimeSpan> GetLatencyAsync();
    Task<Dictionary<string, string>> GetServerInfoAsync();
}

using FlightBooking.Infrastructure.Redis.Configuration;
using FlightBooking.Infrastructure.Redis.Keys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace FlightBooking.Infrastructure.Redis.Services;

/// <summary>
/// Redis service implementation with comprehensive operations
/// </summary>
public class RedisService : IRedisService, IDisposable
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;
    private readonly RedisConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public RedisService(
        IConnectionMultiplexer connection,
        IOptions<RedisConfiguration> config,
        ILogger<RedisService> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _database = _connection.GetDatabase(_config.Database);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _logger.LogInformation("Redis service initialized with database {Database}", _config.Database);
    }

    public IDatabase Database => _database;
    public IConnectionMultiplexer Connection => _connection;
    public bool IsConnected => _connection.IsConnected;

    #region String Operations

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            var result = await _database.StringSetAsync(key, value, expiry);
            _logger.LogDebug("Set string key {Key} with expiry {Expiry}", key, expiry);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set string key {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            _logger.LogDebug("Get string key {Key}, found: {Found}", key, value.HasValue);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get string key {Key}", key);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            var result = await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Deleted key {Key}, success: {Success}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete key {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of key {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expiry)
    {
        try
        {
            return await _database.KeyExpireAsync(key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set expiry for key {Key}", key);
            throw;
        }
    }

    public async Task<TimeSpan?> GetTtlAsync(string key)
    {
        try
        {
            return await _database.KeyTimeToLiveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get TTL for key {Key}", key);
            throw;
        }
    }

    #endregion

    #region Hash Operations

    public async Task<bool> HashSetAsync(string key, string field, string value)
    {
        try
        {
            return await _database.HashSetAsync(key, field, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set hash field {Field} in key {Key}", field, key);
            throw;
        }
    }

    public async Task<bool> HashSetAsync(string key, Dictionary<string, string> values)
    {
        try
        {
            var hashFields = values.Select(kvp => new HashEntry(kvp.Key, kvp.Value)).ToArray();
            await _database.HashSetAsync(key, hashFields);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set hash fields in key {Key}", key);
            throw;
        }
    }

    public async Task<string?> HashGetAsync(string key, string field)
    {
        try
        {
            var value = await _database.HashGetAsync(key, field);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hash field {Field} from key {Key}", field, key);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
    {
        try
        {
            var hashEntries = await _database.HashGetAllAsync(key);
            return hashEntries.ToDictionary(
                entry => entry.Name.ToString(),
                entry => entry.Value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all hash fields from key {Key}", key);
            throw;
        }
    }

    public async Task<bool> HashDeleteAsync(string key, string field)
    {
        try
        {
            return await _database.HashDeleteAsync(key, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete hash field {Field} from key {Key}", field, key);
            throw;
        }
    }

    public async Task<bool> HashExistsAsync(string key, string field)
    {
        try
        {
            return await _database.HashExistsAsync(key, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check hash field {Field} existence in key {Key}", field, key);
            throw;
        }
    }

    #endregion

    #region Set Operations

    public async Task<bool> SetAddAsync(string key, string value)
    {
        try
        {
            return await _database.SetAddAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add value to set {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetRemoveAsync(string key, string value)
    {
        try
        {
            return await _database.SetRemoveAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove value from set {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetContainsAsync(string key, string value)
    {
        try
        {
            return await _database.SetContainsAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check set membership in {Key}", key);
            throw;
        }
    }

    public async Task<string[]> SetMembersAsync(string key)
    {
        try
        {
            var members = await _database.SetMembersAsync(key);
            return members.Select(m => m.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get set members from {Key}", key);
            throw;
        }
    }

    public async Task<long> SetLengthAsync(string key)
    {
        try
        {
            return await _database.SetLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get set length for {Key}", key);
            throw;
        }
    }

    #endregion

    #region List Operations

    public async Task<long> ListPushAsync(string key, string value)
    {
        try
        {
            return await _database.ListLeftPushAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push to list {Key}", key);
            throw;
        }
    }

    public async Task<string?> ListPopAsync(string key)
    {
        try
        {
            var value = await _database.ListLeftPopAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pop from list {Key}", key);
            throw;
        }
    }

    public async Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1)
    {
        try
        {
            var values = await _database.ListRangeAsync(key, start, stop);
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get list range from {Key}", key);
            throw;
        }
    }

    public async Task<long> ListLengthAsync(string key)
    {
        try
        {
            return await _database.ListLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get list length for {Key}", key);
            throw;
        }
    }

    #endregion

    #region Sorted Set Operations

    public async Task<bool> SortedSetAddAsync(string key, string member, double score)
    {
        try
        {
            return await _database.SortedSetAddAsync(key, member, score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add to sorted set {Key}", key);
            throw;
        }
    }

    public async Task<string[]> SortedSetRangeByScoreAsync(string key, double min, double max)
    {
        try
        {
            var values = await _database.SortedSetRangeByScoreAsync(key, min, max);
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sorted set range from {Key}", key);
            throw;
        }
    }

    public async Task<bool> SortedSetRemoveAsync(string key, string member)
    {
        try
        {
            return await _database.SortedSetRemoveAsync(key, member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove from sorted set {Key}", key);
            throw;
        }
    }

    public async Task<long> SortedSetLengthAsync(string key)
    {
        try
        {
            return await _database.SortedSetLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sorted set length for {Key}", key);
            throw;
        }
    }

    #endregion

    #region Atomic Operations

    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        try
        {
            return await _database.StringIncrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment key {Key}", key);
            throw;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1)
    {
        try
        {
            return await _database.StringDecrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrement key {Key}", key);
            throw;
        }
    }

    public async Task<double> IncrementAsync(string key, double value)
    {
        try
        {
            return await _database.StringIncrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment key {Key} by {Value}", key, value);
            throw;
        }
    }

    #endregion

    #region Batch Operations

    public async Task<bool[]> DeleteManyAsync(params string[] keys)
    {
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var deletedCount = await _database.KeyDeleteAsync(redisKeys);
            return keys.Select((_, i) => i < deletedCount).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete multiple keys");
            throw;
        }
    }

    public async Task<string?[]> GetManyAsync(params string[] keys)
    {
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var values = await _database.StringGetAsync(redisKeys);
            return values.Select(v => v.HasValue ? v.ToString() : null).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get multiple keys");
            throw;
        }
    }

    public async Task SetManyAsync(Dictionary<string, string> keyValues, TimeSpan? expiry = null)
    {
        try
        {
            var keyValuePairs = keyValues.Select(kvp => new KeyValuePair<RedisKey, RedisValue>(kvp.Key, kvp.Value)).ToArray();
            await _database.StringSetAsync(keyValuePairs);

            if (expiry.HasValue)
            {
                var tasks = keyValues.Keys.Select(key => _database.KeyExpireAsync(key, expiry.Value));
                await Task.WhenAll(tasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set multiple keys");
            throw;
        }
    }

    #endregion

    #region Pattern Operations

    public async Task<string[]> GetKeysByPatternAsync(string pattern)
    {
        try
        {
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            var keys = server.Keys(_config.Database, pattern);
            return keys.Select(k => k.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get keys by pattern {Pattern}", pattern);
            throw;
        }
    }

    public async Task<long> DeleteByPatternAsync(string pattern)
    {
        try
        {
            var keys = await GetKeysByPatternAsync(pattern);
            if (keys.Length == 0) return 0;

            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            return await _database.KeyDeleteAsync(redisKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete keys by pattern {Pattern}", pattern);
            throw;
        }
    }

    #endregion

    #region Pub/Sub Operations

    public async Task PublishAsync(string channel, string message)
    {
        try
        {
            var subscriber = _connection.GetSubscriber();
            await subscriber.PublishAsync(channel, message);
            _logger.LogDebug("Published message to channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish to channel {Channel}", channel);
            throw;
        }
    }

    public async Task SubscribeAsync(string channel, Action<string, string> handler)
    {
        try
        {
            var subscriber = _connection.GetSubscriber();
            await subscriber.SubscribeAsync(channel, (ch, msg) => handler(ch, msg));
            _logger.LogDebug("Subscribed to channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to channel {Channel}", channel);
            throw;
        }
    }

    public async Task UnsubscribeAsync(string channel)
    {
        try
        {
            var subscriber = _connection.GetSubscriber();
            await subscriber.UnsubscribeAsync(channel);
            _logger.LogDebug("Unsubscribed from channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from channel {Channel}", channel);
            throw;
        }
    }

    #endregion

    #region Transaction Operations

    public async Task<bool> ExecuteTransactionAsync(Func<ITransaction, Task> operations)
    {
        try
        {
            var transaction = _database.CreateTransaction();
            await operations(transaction);
            return await transaction.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute transaction");
            throw;
        }
    }

    #endregion

    #region Lua Script Operations

    public async Task<RedisResult> ExecuteScriptAsync(string script, string[] keys, RedisValue[] values)
    {
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            return await _database.ScriptEvaluateAsync(script, redisKeys, values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Lua script");
            throw;
        }
    }

    #endregion

    #region Health Check Operations

    public async Task<bool> PingAsync()
    {
        try
        {
            var latency = await _database.PingAsync();
            _logger.LogDebug("Redis ping successful, latency: {Latency}ms", latency.TotalMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis ping failed");
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetInfoAsync()
    {
        try
        {
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            var info = await server.InfoAsync();
            return info.SelectMany(g => g).ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Redis info");
            throw;
        }
    }

    public async Task<long> GetDatabaseSizeAsync()
    {
        try
        {
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            return await server.DatabaseSizeAsync(_config.Database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database size");
            throw;
        }
    }

    public async Task<Dictionary<string, long>> GetMemoryUsageAsync()
    {
        try
        {
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            var info = await server.InfoAsync("memory");
            var infoDict = info.SelectMany(g => g).ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "0");

            return new Dictionary<string, long>
            {
                ["used_memory"] = long.Parse(infoDict.GetValueOrDefault("used_memory", "0")),
                ["used_memory_rss"] = long.Parse(infoDict.GetValueOrDefault("used_memory_rss", "0")),
                ["used_memory_peak"] = long.Parse(infoDict.GetValueOrDefault("used_memory_peak", "0")),
                ["maxmemory"] = long.Parse(infoDict.GetValueOrDefault("maxmemory", "0"))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get memory usage");
            throw;
        }
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
            _logger.LogInformation("Redis service disposed");
        }
    }

    #endregion
}

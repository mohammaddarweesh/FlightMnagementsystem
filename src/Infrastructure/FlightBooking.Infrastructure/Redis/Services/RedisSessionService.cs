using FlightBooking.Infrastructure.Redis.Configuration;
using FlightBooking.Infrastructure.Redis.Keys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlightBooking.Infrastructure.Redis.Services;

/// <summary>
/// Redis session service with sliding expiration and minimal PII storage
/// </summary>
public class RedisSessionService : IRedisSessionService
{
    private readonly IRedisService _redisService;
    private readonly ILogger<RedisSessionService> _logger;
    private readonly RedisConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisSessionService(
        IRedisService redisService,
        IOptions<RedisConfiguration> config,
        ILogger<RedisSessionService> logger)
    {
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<string> CreateSessionAsync(bool isAuthenticated = false, Guid? userId = null)
    {
        var sessionId = GenerateSessionId();
        var sessionKey = isAuthenticated && userId.HasValue 
            ? RedisKeyBuilder.UserSessionKey(_config.KeyPrefix, userId.Value)
            : RedisKeyBuilder.GuestSessionKey(_config.KeyPrefix, sessionId);

        var sessionData = new SessionData
        {
            SessionId = sessionId,
            IsAuthenticated = isAuthenticated,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Data = new Dictionary<string, object>()
        };

        var timeout = isAuthenticated 
            ? _config.Session.AuthenticatedSessionTimeout 
            : _config.Session.GuestSessionTimeout;

        try
        {
            var serializedData = JsonSerializer.Serialize(sessionData, _jsonOptions);
            await _redisService.SetStringAsync(sessionKey, serializedData, timeout);

            _logger.LogDebug("Created {SessionType} session {SessionId} with timeout {Timeout}", 
                isAuthenticated ? "authenticated" : "guest", sessionId, timeout);

            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session");
            throw;
        }
    }

    public async Task<bool> ValidateSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            if (sessionData == null)
            {
                _logger.LogDebug("Session {SessionId} not found or expired", sessionId);
                return false;
            }

            // Update last accessed time and refresh expiration (sliding expiration)
            if (_config.Session.SlidingExpiration)
            {
                await RefreshSessionAsync(sessionId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<T?> GetSessionDataAsync<T>(string sessionId, string key) where T : class
    {
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(key))
            return null;

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            if (sessionData?.Data?.TryGetValue(key, out var value) == true)
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _jsonOptions);
                }
                
                if (value is T directValue)
                {
                    return directValue;
                }

                // Try to deserialize from string
                if (value is string stringValue)
                {
                    return JsonSerializer.Deserialize<T>(stringValue, _jsonOptions);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session data for session {SessionId}, key {Key}", sessionId, key);
            return null;
        }
    }

    public async Task<bool> SetSessionDataAsync<T>(string sessionId, string key, T value) where T : class
    {
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(key) || value == null)
            return false;

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            if (sessionData == null)
            {
                _logger.LogWarning("Attempted to set data on non-existent session {SessionId}", sessionId);
                return false;
            }

            // Check session size limits
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            if (serializedValue.Length > _config.Session.MaxSessionSize / 10) // Reserve space for other data
            {
                _logger.LogWarning("Session data too large for session {SessionId}, key {Key}", sessionId, key);
                return false;
            }

            sessionData.Data[key] = value;
            sessionData.LastAccessedAt = DateTime.UtcNow;

            var sessionKey = GetSessionKey(sessionData);
            var serializedSession = JsonSerializer.Serialize(sessionData, _jsonOptions);
            
            var timeout = sessionData.IsAuthenticated 
                ? _config.Session.AuthenticatedSessionTimeout 
                : _config.Session.GuestSessionTimeout;

            await _redisService.SetStringAsync(sessionKey, serializedSession, timeout);

            _logger.LogDebug("Set session data for session {SessionId}, key {Key}", sessionId, key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting session data for session {SessionId}, key {Key}", sessionId, key);
            return false;
        }
    }

    public async Task<bool> RemoveSessionDataAsync(string sessionId, string key)
    {
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(key))
            return false;

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            if (sessionData?.Data?.Remove(key) == true)
            {
                sessionData.LastAccessedAt = DateTime.UtcNow;

                var sessionKey = GetSessionKey(sessionData);
                var serializedSession = JsonSerializer.Serialize(sessionData, _jsonOptions);
                
                var timeout = sessionData.IsAuthenticated 
                    ? _config.Session.AuthenticatedSessionTimeout 
                    : _config.Session.GuestSessionTimeout;

                await _redisService.SetStringAsync(sessionKey, serializedSession, timeout);

                _logger.LogDebug("Removed session data for session {SessionId}, key {Key}", sessionId, key);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing session data for session {SessionId}, key {Key}", sessionId, key);
            return false;
        }
    }

    public async Task<bool> RefreshSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            if (sessionData == null)
                return false;

            sessionData.LastAccessedAt = DateTime.UtcNow;

            var sessionKey = GetSessionKey(sessionData);
            var serializedSession = JsonSerializer.Serialize(sessionData, _jsonOptions);
            
            var timeout = sessionData.IsAuthenticated 
                ? _config.Session.AuthenticatedSessionTimeout 
                : _config.Session.GuestSessionTimeout;

            await _redisService.SetStringAsync(sessionKey, serializedSession, timeout);

            _logger.LogDebug("Refreshed session {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> DestroySessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            if (sessionData == null)
                return false;

            var sessionKey = GetSessionKey(sessionData);
            var deleted = await _redisService.DeleteAsync(sessionKey);

            _logger.LogDebug("Destroyed session {SessionId}, success: {Success}", sessionId, deleted);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error destroying session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetAllSessionDataAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return new Dictionary<string, object>();

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            return sessionData?.Data ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all session data for session {SessionId}", sessionId);
            return new Dictionary<string, object>();
        }
    }

    public async Task<bool> IsSessionExpiredAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return true;

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            return sessionData == null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if session {SessionId} is expired", sessionId);
            return true;
        }
    }

    public async Task<TimeSpan?> GetSessionTtlAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;

        try
        {
            var sessionData = await GetSessionDataInternalAsync(sessionId);
            if (sessionData == null)
                return null;

            var sessionKey = GetSessionKey(sessionData);
            return await _redisService.GetTtlAsync(sessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<long> CleanupExpiredSessionsAsync()
    {
        try
        {
            var guestPattern = RedisKeyBuilder.BuildKey(_config.KeyPrefix, "guest", "session", "*");
            var userPattern = RedisKeyBuilder.BuildKey(_config.KeyPrefix, "user", "session", "*");

            var guestDeleted = await _redisService.DeleteByPatternAsync(guestPattern);
            var userDeleted = await _redisService.DeleteByPatternAsync(userPattern);

            var totalDeleted = guestDeleted + userDeleted;
            _logger.LogInformation("Cleaned up {Count} expired sessions", totalDeleted);
            
            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
            return 0;
        }
    }

    private async Task<SessionData?> GetSessionDataInternalAsync(string sessionId)
    {
        try
        {
            // Try guest session first
            var guestKey = RedisKeyBuilder.GuestSessionKey(_config.KeyPrefix, sessionId);
            var sessionJson = await _redisService.GetStringAsync(guestKey);

            if (sessionJson == null)
            {
                // Try to find user session by scanning (this is expensive, consider indexing)
                var userPattern = RedisKeyBuilder.BuildKey(_config.KeyPrefix, "user", "session", "*");
                var userKeys = await _redisService.GetKeysByPatternAsync(userPattern);
                
                foreach (var userKey in userKeys)
                {
                    var userData = await _redisService.GetStringAsync(userKey);
                    if (userData != null)
                    {
                        var tempSession = JsonSerializer.Deserialize<SessionData>(userData, _jsonOptions);
                        if (tempSession?.SessionId == sessionId)
                        {
                            sessionJson = userData;
                            break;
                        }
                    }
                }
            }

            if (sessionJson == null)
                return null;

            return JsonSerializer.Deserialize<SessionData>(sessionJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session data for session {SessionId}", sessionId);
            return null;
        }
    }

    private string GetSessionKey(SessionData sessionData)
    {
        return sessionData.IsAuthenticated && sessionData.UserId.HasValue
            ? RedisKeyBuilder.UserSessionKey(_config.KeyPrefix, sessionData.UserId.Value)
            : RedisKeyBuilder.GuestSessionKey(_config.KeyPrefix, sessionData.SessionId);
    }

    private static string GenerateSessionId()
    {
        return Guid.NewGuid().ToString("N")[..16]; // 16 character session ID
    }
}

/// <summary>
/// Session data structure with minimal PII
/// </summary>
internal class SessionData
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

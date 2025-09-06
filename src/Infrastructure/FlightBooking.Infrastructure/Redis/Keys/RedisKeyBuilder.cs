using System.Text;

namespace FlightBooking.Infrastructure.Redis.Keys;

/// <summary>
/// Redis key builder with consistent naming conventions
/// </summary>
public static class RedisKeyBuilder
{
    private const string Separator = ":";
    
    /// <summary>
    /// Build a Redis key with consistent naming convention
    /// </summary>
    /// <param name="prefix">Key prefix (e.g., "flightbooking")</param>
    /// <param name="parts">Key parts to join</param>
    /// <returns>Formatted Redis key</returns>
    public static string BuildKey(string prefix, params string[] parts)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));

        if (parts == null || parts.Length == 0)
            throw new ArgumentException("At least one key part is required", nameof(parts));

        var keyBuilder = new StringBuilder(prefix);
        
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
                throw new ArgumentException("Key parts cannot be null or empty");
                
            keyBuilder.Append(Separator).Append(part.ToLowerInvariant());
        }

        return keyBuilder.ToString();
    }

    /// <summary>
    /// Build a session key
    /// </summary>
    public static string SessionKey(string prefix, string sessionId)
        => BuildKey(prefix, "session", sessionId);

    /// <summary>
    /// Build a cache key
    /// </summary>
    public static string CacheKey(string prefix, string category, string identifier)
        => BuildKey(prefix, "cache", category, identifier);

    /// <summary>
    /// Build a lock key
    /// </summary>
    public static string LockKey(string prefix, string resource, string identifier)
        => BuildKey(prefix, "locks", resource, identifier);

    /// <summary>
    /// Build a rate limit key
    /// </summary>
    public static string RateLimitKey(string prefix, string endpoint, string identifier, long windowStart)
        => BuildKey(prefix, "ratelimit", endpoint, identifier, windowStart.ToString());

    /// <summary>
    /// Build a metrics key
    /// </summary>
    public static string MetricsKey(string prefix, string metricType, string identifier)
        => BuildKey(prefix, "metrics", metricType, identifier);

    /// <summary>
    /// Build a booking key
    /// </summary>
    public static string BookingKey(string prefix, Guid bookingId)
        => BuildKey(prefix, "booking", bookingId.ToString());

    /// <summary>
    /// Build a seat allocation key
    /// </summary>
    public static string SeatAllocationKey(string prefix, Guid flightId, string seatNumber)
        => BuildKey(prefix, "seat", flightId.ToString(), seatNumber);

    /// <summary>
    /// Build a promotion key
    /// </summary>
    public static string PromotionKey(string prefix, string promoCode)
        => BuildKey(prefix, "promo", promoCode);

    /// <summary>
    /// Build a user session key
    /// </summary>
    public static string UserSessionKey(string prefix, Guid userId)
        => BuildKey(prefix, "user", "session", userId.ToString());

    /// <summary>
    /// Build a guest session key
    /// </summary>
    public static string GuestSessionKey(string prefix, string guestId)
        => BuildKey(prefix, "guest", "session", guestId);

    /// <summary>
    /// Build a flight availability key
    /// </summary>
    public static string FlightAvailabilityKey(string prefix, string route, DateTime date)
        => BuildKey(prefix, "flight", "availability", route, date.ToString("yyyy-MM-dd"));

    /// <summary>
    /// Build a pricing key
    /// </summary>
    public static string PricingKey(string prefix, Guid flightId, string fareClass)
        => BuildKey(prefix, "pricing", flightId.ToString(), fareClass);

    /// <summary>
    /// Extract parts from a Redis key
    /// </summary>
    public static string[] ExtractParts(string key)
    {
        if (string.IsNullOrEmpty(key))
            return Array.Empty<string>();

        return key.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Get the category from a key
    /// </summary>
    public static string? GetCategory(string key)
    {
        var parts = ExtractParts(key);
        return parts.Length >= 2 ? parts[1] : null;
    }

    /// <summary>
    /// Check if key matches pattern
    /// </summary>
    public static bool MatchesPattern(string key, string pattern)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(pattern))
            return false;

        // Simple wildcard matching (* and ?)
        return System.Text.RegularExpressions.Regex.IsMatch(
            key, 
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}

/// <summary>
/// Redis key constants and patterns
/// </summary>
public static class RedisKeyPatterns
{
    // Session patterns
    public const string AllSessions = "*:session:*";
    public const string UserSessions = "*:user:session:*";
    public const string GuestSessions = "*:guest:session:*";

    // Cache patterns
    public const string AllCache = "*:cache:*";
    public const string BookingCache = "*:cache:booking:*";
    public const string FlightCache = "*:cache:flight:*";

    // Lock patterns
    public const string AllLocks = "*:locks:*";
    public const string SeatLocks = "*:locks:seat:*";
    public const string PromoLocks = "*:locks:promo:*";

    // Rate limit patterns
    public const string AllRateLimits = "*:ratelimit:*";
    public const string BookingRateLimits = "*:ratelimit:booking:*";

    // Metrics patterns
    public const string AllMetrics = "*:metrics:*";
    public const string CacheMetrics = "*:metrics:cache:*";
    public const string LockMetrics = "*:metrics:lock:*";
}

/// <summary>
/// TTL constants for different types of data
/// </summary>
public static class RedisTtl
{
    // Session TTLs
    public static readonly TimeSpan GuestSession = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan UserSession = TimeSpan.FromHours(2);
    public static readonly TimeSpan AdminSession = TimeSpan.FromHours(8);

    // Cache TTLs
    public static readonly TimeSpan ShortCache = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MediumCache = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan LongCache = TimeSpan.FromHours(2);
    public static readonly TimeSpan VeryLongCache = TimeSpan.FromHours(24);

    // Lock TTLs
    public static readonly TimeSpan ShortLock = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MediumLock = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan LongLock = TimeSpan.FromMinutes(10);

    // Rate limit TTLs
    public static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan RateLimitBurst = TimeSpan.FromSeconds(10);

    // Metrics TTLs
    public static readonly TimeSpan Metrics = TimeSpan.FromDays(7);
    public static readonly TimeSpan ShortMetrics = TimeSpan.FromHours(1);
}

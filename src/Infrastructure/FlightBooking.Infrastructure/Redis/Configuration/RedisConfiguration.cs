using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Infrastructure.Redis.Configuration;

/// <summary>
/// Redis configuration settings with validation
/// </summary>
public class RedisConfiguration
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis connection string
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Database number to use (0-15)
    /// </summary>
    [Range(0, 15)]
    public int Database { get; set; } = 0;

    /// <summary>
    /// Key prefix for all Redis keys
    /// </summary>
    [Required]
    public string KeyPrefix { get; set; } = "flightbooking";

    /// <summary>
    /// Default TTL for cached items
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Connection timeout
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Command timeout
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Retry count for failed operations
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Enable Redis clustering
    /// </summary>
    public bool EnableClustering { get; set; } = false;

    /// <summary>
    /// Session configuration
    /// </summary>
    public SessionConfiguration Session { get; set; } = new();

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public RateLimitConfiguration RateLimit { get; set; } = new();

    /// <summary>
    /// Distributed lock configuration
    /// </summary>
    public DistributedLockConfiguration DistributedLock { get; set; } = new();

    /// <summary>
    /// Metrics configuration
    /// </summary>
    public MetricsConfiguration Metrics { get; set; } = new();
}

/// <summary>
/// Session-specific Redis configuration
/// </summary>
public class SessionConfiguration
{
    /// <summary>
    /// Session timeout for guests
    /// </summary>
    public TimeSpan GuestSessionTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Session timeout for authenticated users
    /// </summary>
    public TimeSpan AuthenticatedSessionTimeout { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Sliding expiration enabled
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;

    /// <summary>
    /// Cookie name for session ID
    /// </summary>
    public string CookieName { get; set; } = "FlightBooking.SessionId";

    /// <summary>
    /// Maximum data size per session (in bytes)
    /// </summary>
    public int MaxSessionSize { get; set; } = 1024 * 1024; // 1MB
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Default permit limit
    /// </summary>
    public int DefaultPermitLimit { get; set; } = 100;

    /// <summary>
    /// Default time window
    /// </summary>
    public TimeSpan DefaultWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Segments per window for sliding window
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 8;

    /// <summary>
    /// Rate limit policies for specific endpoints
    /// </summary>
    public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new();
}

/// <summary>
/// Rate limit policy for specific endpoints
/// </summary>
public class RateLimitPolicy
{
    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
    public int SegmentsPerWindow { get; set; } = 8;
}

/// <summary>
/// Distributed lock configuration
/// </summary>
public class DistributedLockConfiguration
{
    /// <summary>
    /// Default lock timeout
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default retry delay
    /// </summary>
    public TimeSpan DefaultRetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 10;

    /// <summary>
    /// Enable jitter for retry delays
    /// </summary>
    public bool EnableJitter { get; set; } = true;

    /// <summary>
    /// Jitter factor (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public double JitterFactor { get; set; } = 0.2;

    /// <summary>
    /// Lock key prefix
    /// </summary>
    public string LockKeyPrefix { get; set; } = "locks";
}

/// <summary>
/// Metrics configuration
/// </summary>
public class MetricsConfiguration
{
    /// <summary>
    /// Enable cache hit ratio metrics
    /// </summary>
    public bool EnableCacheMetrics { get; set; } = true;

    /// <summary>
    /// Enable lock contention metrics
    /// </summary>
    public bool EnableLockMetrics { get; set; } = true;

    /// <summary>
    /// Enable performance metrics
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Metrics collection interval
    /// </summary>
    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Metrics retention period
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
}

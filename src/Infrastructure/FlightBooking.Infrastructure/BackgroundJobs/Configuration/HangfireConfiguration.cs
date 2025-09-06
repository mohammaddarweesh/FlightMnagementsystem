namespace FlightBooking.Infrastructure.BackgroundJobs.Configuration;

/// <summary>
/// Hangfire configuration settings
/// </summary>
public class HangfireConfiguration
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// PostgreSQL connection string for Hangfire storage
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database schema name for Hangfire tables
    /// </summary>
    public string SchemaName { get; set; } = "hangfire";

    /// <summary>
    /// Dashboard configuration
    /// </summary>
    public DashboardConfiguration Dashboard { get; set; } = new();

    /// <summary>
    /// Server configuration
    /// </summary>
    public ServerConfiguration Server { get; set; } = new();

    /// <summary>
    /// Queue configurations
    /// </summary>
    public QueueConfiguration Queues { get; set; } = new();

    /// <summary>
    /// Retry policy configuration
    /// </summary>
    public RetryConfiguration Retry { get; set; } = new();

    /// <summary>
    /// Dead letter queue configuration
    /// </summary>
    public DeadLetterQueueConfiguration DeadLetterQueue { get; set; } = new();

    /// <summary>
    /// Job timeout configuration
    /// </summary>
    public TimeoutConfiguration Timeouts { get; set; } = new();
}

/// <summary>
/// Dashboard configuration
/// </summary>
public class DashboardConfiguration
{
    /// <summary>
    /// Enable Hangfire dashboard
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Dashboard URL path
    /// </summary>
    public string Path { get; set; } = "/hangfire";

    /// <summary>
    /// Require authentication for dashboard access
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Required authorization policy for dashboard access
    /// </summary>
    public string RequiredPolicy { get; set; } = "Admin";

    /// <summary>
    /// Dashboard title
    /// </summary>
    public string Title { get; set; } = "Flight Booking Background Jobs";

    /// <summary>
    /// Enable dark theme
    /// </summary>
    public bool DarkTheme { get; set; } = false;
}

/// <summary>
/// Server configuration
/// </summary>
public class ServerConfiguration
{
    /// <summary>
    /// Server name identifier
    /// </summary>
    public string ServerName { get; set; } = Environment.MachineName;

    /// <summary>
    /// Number of worker threads
    /// </summary>
    public int WorkerCount { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Queues to process (in priority order)
    /// </summary>
    public string[] Queues { get; set; } = { "critical", "emails", "reports", "cleanup", "pricing", "default" };

    /// <summary>
    /// Shutdown timeout
    /// </summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Heartbeat interval
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Server check interval
    /// </summary>
    public TimeSpan ServerCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Queue configuration
/// </summary>
public class QueueConfiguration
{
    /// <summary>
    /// Critical queue for urgent operations
    /// </summary>
    public QueueSettings Critical { get; set; } = new() { Name = "critical", Priority = 1, MaxWorkers = 2 };

    /// <summary>
    /// Email queue for email processing
    /// </summary>
    public QueueSettings Emails { get; set; } = new() { Name = "emails", Priority = 2, MaxWorkers = 3 };

    /// <summary>
    /// Reports queue for report generation
    /// </summary>
    public QueueSettings Reports { get; set; } = new() { Name = "reports", Priority = 3, MaxWorkers = 2 };

    /// <summary>
    /// Analytics queue for analytics processing
    /// </summary>
    public QueueSettings Analytics { get; set; } = new() { Name = "analytics", Priority = 4, MaxWorkers = 2 };

    /// <summary>
    /// Cleanup queue for maintenance tasks
    /// </summary>
    public QueueSettings Cleanup { get; set; } = new() { Name = "cleanup", Priority = 5, MaxWorkers = 1 };

    /// <summary>
    /// Pricing queue for pricing calculations
    /// </summary>
    public QueueSettings Pricing { get; set; } = new() { Name = "pricing", Priority = 6, MaxWorkers = 2 };

    /// <summary>
    /// Default queue for general tasks
    /// </summary>
    public QueueSettings Default { get; set; } = new() { Name = "default", Priority = 7, MaxWorkers = 2 };
}

/// <summary>
/// Individual queue settings
/// </summary>
public class QueueSettings
{
    /// <summary>
    /// Queue name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Queue priority (lower number = higher priority)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Maximum number of workers for this queue
    /// </summary>
    public int MaxWorkers { get; set; } = 1;

    /// <summary>
    /// Queue-specific timeout
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

/// <summary>
/// Retry configuration
/// </summary>
public class RetryConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Base delay for exponential backoff (in seconds)
    /// </summary>
    public int BaseDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Maximum delay between retries (in seconds)
    /// </summary>
    public int MaxDelaySeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Exponential backoff multiplier
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Add jitter to retry delays
    /// </summary>
    public bool EnableJitter { get; set; } = true;

    /// <summary>
    /// Jitter factor (0.0 to 1.0)
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;
}

/// <summary>
/// Dead letter queue configuration
/// </summary>
public class DeadLetterQueueConfiguration
{
    /// <summary>
    /// Enable dead letter queue
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Table name for dead letter queue
    /// </summary>
    public string TableName { get; set; } = "job_dead_letter_queue";

    /// <summary>
    /// Retention period for dead letter entries
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Enable automatic cleanup of old dead letter entries
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Cleanup interval
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
}

/// <summary>
/// Timeout configuration
/// </summary>
public class TimeoutConfiguration
{
    /// <summary>
    /// Default job execution timeout
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Email job timeout
    /// </summary>
    public TimeSpan EmailTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Report generation timeout
    /// </summary>
    public TimeSpan ReportTimeout { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Cleanup job timeout
    /// </summary>
    public TimeSpan CleanupTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Pricing calculation timeout
    /// </summary>
    public TimeSpan PricingTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Critical job timeout
    /// </summary>
    public TimeSpan CriticalTimeout { get; set; } = TimeSpan.FromMinutes(10);
}

/// <summary>
/// Hangfire queue names constants
/// </summary>
public static class HangfireQueues
{
    public const string Critical = "critical";
    public const string Emails = "emails";
    public const string Reports = "reports";
    public const string Analytics = "analytics";
    public const string Cleanup = "cleanup";
    public const string Pricing = "pricing";
    public const string Default = "default";

    /// <summary>
    /// Get all queue names in priority order
    /// </summary>
    public static string[] All => new[] { Critical, Emails, Reports, Analytics, Cleanup, Pricing, Default };

    /// <summary>
    /// Get high priority queues
    /// </summary>
    public static string[] HighPriority => new[] { Critical, Emails };

    /// <summary>
    /// Get medium priority queues
    /// </summary>
    public static string[] MediumPriority => new[] { Reports, Analytics };

    /// <summary>
    /// Get low priority queues
    /// </summary>
    public static string[] LowPriority => new[] { Cleanup, Pricing, Default };
}

using RedLockNet;

namespace FlightBooking.Infrastructure.Redis.Services;

/// <summary>
/// Distributed lock service interface using RedLock algorithm
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Acquire a distributed lock with default timeout
    /// </summary>
    Task<IRedLock?> AcquireLockAsync(string resource, string? lockId = null);

    /// <summary>
    /// Acquire a distributed lock with custom timeout
    /// </summary>
    Task<IRedLock?> AcquireLockAsync(string resource, TimeSpan timeout, string? lockId = null);

    /// <summary>
    /// Acquire a distributed lock with custom timeout and retry settings
    /// </summary>
    Task<IRedLock?> AcquireLockAsync(string resource, TimeSpan timeout, TimeSpan retryDelay, int maxRetryAttempts, string? lockId = null);

    /// <summary>
    /// Try to acquire a lock without retries
    /// </summary>
    Task<IRedLock?> TryAcquireLockAsync(string resource, TimeSpan timeout, string? lockId = null);

    /// <summary>
    /// Execute an action within a distributed lock
    /// </summary>
    Task<T> ExecuteWithLockAsync<T>(string resource, Func<Task<T>> action, TimeSpan? timeout = null);

    /// <summary>
    /// Execute an action within a distributed lock (void return)
    /// </summary>
    Task ExecuteWithLockAsync(string resource, Func<Task> action, TimeSpan? timeout = null);

    /// <summary>
    /// Execute an action within a distributed lock with retry logic
    /// </summary>
    Task<T> ExecuteWithLockAsync<T>(string resource, Func<Task<T>> action, TimeSpan timeout, TimeSpan retryDelay, int maxRetryAttempts);

    /// <summary>
    /// Check if a resource is currently locked
    /// </summary>
    Task<bool> IsLockedAsync(string resource);

    /// <summary>
    /// Get lock information for a resource
    /// </summary>
    Task<LockInfo?> GetLockInfoAsync(string resource);

    /// <summary>
    /// Force release a lock (use with caution)
    /// </summary>
    Task<bool> ForceReleaseLockAsync(string resource);

    /// <summary>
    /// Get all active locks
    /// </summary>
    Task<IEnumerable<LockInfo>> GetActiveLocksAsync();

    /// <summary>
    /// Cleanup expired locks
    /// </summary>
    Task<int> CleanupExpiredLocksAsync();
}

/// <summary>
/// Lock information
/// </summary>
public class LockInfo
{
    public string Resource { get; set; } = string.Empty;
    public string LockId { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Owner { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public TimeSpan RemainingTime => ExpiresAt > DateTime.UtcNow ? ExpiresAt - DateTime.UtcNow : TimeSpan.Zero;
}

/// <summary>
/// Lock acquisition result
/// </summary>
public class LockResult
{
    public bool Success { get; set; }
    public IRedLock? Lock { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan AcquisitionTime { get; set; }
    public int RetryAttempts { get; set; }

    public static LockResult Successful(IRedLock redLock, TimeSpan acquisitionTime, int retryAttempts = 0)
        => new() { Success = true, Lock = redLock, AcquisitionTime = acquisitionTime, RetryAttempts = retryAttempts };

    public static LockResult Failed(string errorMessage, TimeSpan acquisitionTime, int retryAttempts = 0)
        => new() { Success = false, ErrorMessage = errorMessage, AcquisitionTime = acquisitionTime, RetryAttempts = retryAttempts };
}

/// <summary>
/// Lock contention metrics
/// </summary>
public class LockContentionMetrics
{
    public string Resource { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int SuccessfulAcquisitions { get; set; }
    public int FailedAcquisitions { get; set; }
    public int TimeoutOccurrences { get; set; }
    public TimeSpan AverageAcquisitionTime { get; set; }
    public TimeSpan MaxAcquisitionTime { get; set; }
    public TimeSpan MinAcquisitionTime { get; set; }
    public DateTime LastAttempt { get; set; }
    public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulAcquisitions / TotalAttempts : 0;
    public double ContentionRate => TotalAttempts > 0 ? (double)FailedAcquisitions / TotalAttempts : 0;
}

/// <summary>
/// Specialized lock service for seat allocation
/// </summary>
public interface ISeatLockService
{
    Task<IRedLock?> AcquireSeatLockAsync(Guid flightId, string seatNumber, TimeSpan? timeout = null);
    Task<bool> IsSeatLockedAsync(Guid flightId, string seatNumber);
    Task<T> ExecuteWithSeatLockAsync<T>(Guid flightId, string seatNumber, Func<Task<T>> action);
    Task<Dictionary<string, bool>> GetSeatLockStatusAsync(Guid flightId, IEnumerable<string> seatNumbers);
    Task<int> ReleaseSeatLocksForFlightAsync(Guid flightId);
}

/// <summary>
/// Specialized lock service for promotion redemption
/// </summary>
public interface IPromotionLockService
{
    Task<IRedLock?> AcquirePromotionLockAsync(string promoCode, Guid? customerId = null, TimeSpan? timeout = null);
    Task<bool> IsPromotionLockedAsync(string promoCode);
    Task<T> ExecuteWithPromotionLockAsync<T>(string promoCode, Func<Task<T>> action, Guid? customerId = null);
    Task<LockContentionMetrics> GetPromotionContentionMetricsAsync(string promoCode);
    Task<int> CleanupPromotionLocksAsync();
}

namespace FlightBooking.Infrastructure.BackgroundJobs.DeadLetterQueue;

/// <summary>
/// Service for managing dead letter queue entries
/// </summary>
public interface IDeadLetterQueueService
{
    /// <summary>
    /// Add a failed job to the dead letter queue
    /// </summary>
    Task AddToDeadLetterQueueAsync(
        string jobId,
        string? correlationId,
        string jobType,
        string methodName,
        string? arguments,
        string queueName,
        int retryAttempts,
        Exception exception,
        DateTime createdAt,
        DateTime firstFailedAt,
        string? serverName = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dead letter queue entries with pagination and filtering
    /// </summary>
    Task<DeadLetterQueueResult> GetEntriesAsync(
        DeadLetterQueueQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific dead letter queue entry by ID
    /// </summary>
    Task<DeadLetterQueueEntry?> GetEntryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dead letter queue statistics
    /// </summary>
    Task<DeadLetterQueueStats> GetStatsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requeue a dead letter entry back to Hangfire
    /// </summary>
    Task<bool> RequeueEntryAsync(
        Guid id,
        string requeuedBy,
        string? newQueueName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requeue multiple dead letter entries
    /// </summary>
    Task<int> RequeueEntriesAsync(
        IEnumerable<Guid> ids,
        string requeuedBy,
        string? newQueueName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a dead letter queue entry
    /// </summary>
    Task<bool> DeleteEntryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete multiple dead letter queue entries
    /// </summary>
    Task<int> DeleteEntriesAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old dead letter queue entries
    /// </summary>
    Task<int> CleanupOldEntriesAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entries by correlation ID
    /// </summary>
    Task<List<DeadLetterQueueEntry>> GetEntriesByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entries by job type
    /// </summary>
    Task<List<DeadLetterQueueEntry>> GetEntriesByJobTypeAsync(
        string jobType,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entries by queue name
    /// </summary>
    Task<List<DeadLetterQueueEntry>> GetEntriesByQueueAsync(
        string queueName,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent failures (last 24 hours)
    /// </summary>
    Task<List<DeadLetterQueueEntry>> GetRecentFailuresAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failure trends by hour for the last 24 hours
    /// </summary>
    Task<Dictionary<DateTime, int>> GetFailureTrendsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top failing job types
    /// </summary>
    Task<Dictionary<string, int>> GetTopFailingJobTypesAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top exception types
    /// </summary>
    Task<Dictionary<string, int>> GetTopExceptionTypesAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a job ID exists in the dead letter queue
    /// </summary>
    Task<bool> ExistsAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the count of entries in dead letter queue
    /// </summary>
    Task<int> GetCountAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the count of entries by queue
    /// </summary>
    Task<Dictionary<string, int>> GetCountByQueueAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export dead letter queue entries to CSV
    /// </summary>
    Task<byte[]> ExportToCsvAsync(
        DeadLetterQueueQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk requeue entries based on criteria
    /// </summary>
    Task<int> BulkRequeueAsync(
        string? queueName = null,
        string? jobType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string requeuedBy = "System",
        string? newQueueName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk delete entries based on criteria
    /// </summary>
    Task<int> BulkDeleteAsync(
        string? queueName = null,
        string? jobType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool includeRequeued = false,
        CancellationToken cancellationToken = default);
}

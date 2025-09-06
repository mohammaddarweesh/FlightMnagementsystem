using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Infrastructure.BackgroundJobs.DeadLetterQueue;

/// <summary>
/// Dead letter queue entry for failed jobs
/// </summary>
[Table("job_dead_letter_queue", Schema = "hangfire")]
public class DeadLetterQueueEntry
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Original Hangfire job ID
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Job type name
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Job method name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Serialized job arguments
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Queue name where the job was processed
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Final exception message
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// Full exception details
    /// </summary>
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// When the job was originally created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the job first failed
    /// </summary>
    public DateTime FirstFailedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the job was moved to dead letter queue
    /// </summary>
    public DateTime MovedToDeadLetterAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Server name that processed the job
    /// </summary>
    [MaxLength(200)]
    public string? ServerName { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this entry has been requeued
    /// </summary>
    public bool IsRequeued { get; set; }

    /// <summary>
    /// When this entry was requeued (if applicable)
    /// </summary>
    public DateTime? RequeuedAt { get; set; }

    /// <summary>
    /// Who requeued this entry
    /// </summary>
    [MaxLength(200)]
    public string? RequeuedBy { get; set; }

    /// <summary>
    /// Mark this entry as requeued
    /// </summary>
    public void MarkAsRequeued(string requeuedBy)
    {
        IsRequeued = true;
        RequeuedAt = DateTime.UtcNow;
        RequeuedBy = requeuedBy;
    }

    /// <summary>
    /// Create a dead letter queue entry from job information
    /// </summary>
    public static DeadLetterQueueEntry FromJobFailure(
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
        string? metadata = null)
    {
        return new DeadLetterQueueEntry
        {
            JobId = jobId,
            CorrelationId = correlationId,
            JobType = jobType,
            MethodName = methodName,
            Arguments = arguments,
            QueueName = queueName,
            RetryAttempts = retryAttempts,
            ExceptionMessage = exception.Message,
            ExceptionDetails = exception.ToString(),
            CreatedAt = createdAt,
            FirstFailedAt = firstFailedAt,
            ServerName = serverName,
            Metadata = metadata
        };
    }
}

/// <summary>
/// Dead letter queue statistics
/// </summary>
public class DeadLetterQueueStats
{
    /// <summary>
    /// Total number of entries in dead letter queue
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Number of entries by queue
    /// </summary>
    public Dictionary<string, int> EntriesByQueue { get; set; } = new();

    /// <summary>
    /// Number of entries by job type
    /// </summary>
    public Dictionary<string, int> EntriesByJobType { get; set; } = new();

    /// <summary>
    /// Number of entries by exception type
    /// </summary>
    public Dictionary<string, int> EntriesByExceptionType { get; set; } = new();

    /// <summary>
    /// Entries added in the last 24 hours
    /// </summary>
    public int EntriesLast24Hours { get; set; }

    /// <summary>
    /// Entries added in the last 7 days
    /// </summary>
    public int EntriesLast7Days { get; set; }

    /// <summary>
    /// Number of requeued entries
    /// </summary>
    public int RequeuedEntries { get; set; }

    /// <summary>
    /// Oldest entry date
    /// </summary>
    public DateTime? OldestEntryDate { get; set; }

    /// <summary>
    /// Most recent entry date
    /// </summary>
    public DateTime? MostRecentEntryDate { get; set; }
}

/// <summary>
/// Dead letter queue query parameters
/// </summary>
public class DeadLetterQueueQuery
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Filter by queue name
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Filter by job type
    /// </summary>
    public string? JobType { get; set; }

    /// <summary>
    /// Filter by exception message (contains)
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// Filter by date range - from
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by date range - to
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Include requeued entries
    /// </summary>
    public bool IncludeRequeued { get; set; } = true;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "MovedToDeadLetterAt";

    /// <summary>
    /// Sort direction
    /// </summary>
    public string SortDirection { get; set; } = "DESC";
}

/// <summary>
/// Dead letter queue query result
/// </summary>
public class DeadLetterQueueResult
{
    /// <summary>
    /// Entries for the current page
    /// </summary>
    public List<DeadLetterQueueEntry> Entries { get; set; } = new();

    /// <summary>
    /// Total number of entries matching the query
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there are previous pages
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

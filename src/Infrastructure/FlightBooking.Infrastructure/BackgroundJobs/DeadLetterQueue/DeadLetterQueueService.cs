using FlightBooking.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FlightBooking.Infrastructure.BackgroundJobs.DeadLetterQueue;

/// <summary>
/// Service for managing dead letter queue entries
/// </summary>
public class DeadLetterQueueService : IDeadLetterQueueService
{
    private readonly ApplicationDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<DeadLetterQueueService> _logger;

    public DeadLetterQueueService(
        ApplicationDbContext context,
        IBackgroundJobClient backgroundJobClient,
        ILogger<DeadLetterQueueService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddToDeadLetterQueueAsync(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = DeadLetterQueueEntry.FromJobFailure(
                jobId, correlationId, jobType, methodName, arguments,
                queueName, retryAttempts, exception, createdAt, firstFailedAt,
                serverName, metadata);

            _context.Set<DeadLetterQueueEntry>().Add(entry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Job {JobId} moved to dead letter queue after {RetryAttempts} attempts. Exception: {Exception}",
                jobId, retryAttempts, exception.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add job {JobId} to dead letter queue", jobId);
            throw;
        }
    }

    public async Task<DeadLetterQueueResult> GetEntriesAsync(
        DeadLetterQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.Set<DeadLetterQueueEntry>().AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(query.QueueName))
        {
            queryable = queryable.Where(e => e.QueueName == query.QueueName);
        }

        if (!string.IsNullOrEmpty(query.JobType))
        {
            queryable = queryable.Where(e => e.JobType.Contains(query.JobType));
        }

        if (!string.IsNullOrEmpty(query.ExceptionMessage))
        {
            queryable = queryable.Where(e => e.ExceptionMessage != null && 
                e.ExceptionMessage.Contains(query.ExceptionMessage));
        }

        if (query.FromDate.HasValue)
        {
            queryable = queryable.Where(e => e.MovedToDeadLetterAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            queryable = queryable.Where(e => e.MovedToDeadLetterAt <= query.ToDate.Value);
        }

        if (!query.IncludeRequeued)
        {
            queryable = queryable.Where(e => !e.IsRequeued);
        }

        // Get total count
        var totalCount = await queryable.CountAsync(cancellationToken);

        // Apply sorting
        queryable = query.SortBy.ToLower() switch
        {
            "createdat" => query.SortDirection.ToUpper() == "ASC" 
                ? queryable.OrderBy(e => e.CreatedAt)
                : queryable.OrderByDescending(e => e.CreatedAt),
            "firstfailedat" => query.SortDirection.ToUpper() == "ASC"
                ? queryable.OrderBy(e => e.FirstFailedAt)
                : queryable.OrderByDescending(e => e.FirstFailedAt),
            "retryattempts" => query.SortDirection.ToUpper() == "ASC"
                ? queryable.OrderBy(e => e.RetryAttempts)
                : queryable.OrderByDescending(e => e.RetryAttempts),
            "queuename" => query.SortDirection.ToUpper() == "ASC"
                ? queryable.OrderBy(e => e.QueueName)
                : queryable.OrderByDescending(e => e.QueueName),
            "jobtype" => query.SortDirection.ToUpper() == "ASC"
                ? queryable.OrderBy(e => e.JobType)
                : queryable.OrderByDescending(e => e.JobType),
            _ => query.SortDirection.ToUpper() == "ASC"
                ? queryable.OrderBy(e => e.MovedToDeadLetterAt)
                : queryable.OrderByDescending(e => e.MovedToDeadLetterAt)
        };

        // Apply pagination
        var entries = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new DeadLetterQueueResult
        {
            Entries = entries,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<DeadLetterQueueEntry?> GetEntryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeadLetterQueueEntry>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<DeadLetterQueueStats> GetStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = _context.Set<DeadLetterQueueEntry>().AsQueryable();
        var now = DateTime.UtcNow;

        var stats = new DeadLetterQueueStats
        {
            TotalEntries = await entries.CountAsync(cancellationToken),
            EntriesByQueue = await entries
                .GroupBy(e => e.QueueName)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken),
            EntriesByJobType = await entries
                .GroupBy(e => e.JobType)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken),
            EntriesLast24Hours = await entries
                .Where(e => e.MovedToDeadLetterAt >= now.AddDays(-1))
                .CountAsync(cancellationToken),
            EntriesLast7Days = await entries
                .Where(e => e.MovedToDeadLetterAt >= now.AddDays(-7))
                .CountAsync(cancellationToken),
            RequeuedEntries = await entries
                .Where(e => e.IsRequeued)
                .CountAsync(cancellationToken)
        };

        // Get exception types
        var exceptionTypes = await entries
            .Where(e => e.ExceptionMessage != null)
            .Select(e => e.ExceptionMessage!.Split(new char[] { ':' }, StringSplitOptions.None)[0].Trim())
            .GroupBy(e => e)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

        stats.EntriesByExceptionType = exceptionTypes;

        // Get date ranges
        var dateInfo = await entries
            .Select(e => e.MovedToDeadLetterAt)
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);

        if (dateInfo.Any())
        {
            stats.OldestEntryDate = dateInfo.First();
            stats.MostRecentEntryDate = dateInfo.Last();
        }

        return stats;
    }

    public async Task<bool> RequeueEntryAsync(
        Guid id,
        string requeuedBy,
        string? newQueueName = null,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryByIdAsync(id, cancellationToken);
        if (entry == null || entry.IsRequeued)
        {
            return false;
        }

        try
        {
            // Deserialize arguments if available
            object[]? args = null;
            if (!string.IsNullOrEmpty(entry.Arguments))
            {
                args = JsonSerializer.Deserialize<object[]>(entry.Arguments);
            }

            // Requeue the job
            var queueName = newQueueName ?? entry.QueueName;
            var jobId = _backgroundJobClient.Enqueue(queueName, () => 
                RequeuedJobPlaceholder(entry.JobType, entry.MethodName, args));

            // Mark as requeued
            entry.MarkAsRequeued(requeuedBy);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Dead letter entry {Id} requeued as job {JobId} by {RequeuedBy}",
                id, jobId, requeuedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to requeue dead letter entry {Id}", id);
            return false;
        }
    }

    public async Task<int> RequeueEntriesAsync(
        IEnumerable<Guid> ids,
        string requeuedBy,
        string? newQueueName = null,
        CancellationToken cancellationToken = default)
    {
        var requeuedCount = 0;
        foreach (var id in ids)
        {
            if (await RequeueEntryAsync(id, requeuedBy, newQueueName, cancellationToken))
            {
                requeuedCount++;
            }
        }
        return requeuedCount;
    }

    public async Task<bool> DeleteEntryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryByIdAsync(id, cancellationToken);
        if (entry == null)
        {
            return false;
        }

        _context.Set<DeadLetterQueueEntry>().Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dead letter entry {Id} deleted", id);
        return true;
    }

    public async Task<int> DeleteEntriesAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var entries = await _context.Set<DeadLetterQueueEntry>()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);

        if (entries.Any())
        {
            _context.Set<DeadLetterQueueEntry>().RemoveRange(entries);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} dead letter entries", entries.Count);
        }

        return entries.Count;
    }

    public async Task<int> CleanupOldEntriesAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;
        var oldEntries = await _context.Set<DeadLetterQueueEntry>()
            .Where(e => e.MovedToDeadLetterAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldEntries.Any())
        {
            _context.Set<DeadLetterQueueEntry>().RemoveRange(oldEntries);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} old dead letter entries older than {CutoffDate}",
                oldEntries.Count, cutoffDate);
        }

        return oldEntries.Count;
    }

    // Additional methods implementation continues...
    public async Task<List<DeadLetterQueueEntry>> GetEntriesByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeadLetterQueueEntry>()
            .Where(e => e.CorrelationId == correlationId)
            .OrderByDescending(e => e.MovedToDeadLetterAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeadLetterQueueEntry>> GetEntriesByJobTypeAsync(
        string jobType,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeadLetterQueueEntry>()
            .Where(e => e.JobType.Contains(jobType))
            .OrderByDescending(e => e.MovedToDeadLetterAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeadLetterQueueEntry>> GetEntriesByQueueAsync(
        string queueName,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeadLetterQueueEntry>()
            .Where(e => e.QueueName == queueName)
            .OrderByDescending(e => e.MovedToDeadLetterAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeadLetterQueueEntry>> GetRecentFailuresAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-1);
        return await _context.Set<DeadLetterQueueEntry>()
            .Where(e => e.MovedToDeadLetterAt >= since)
            .OrderByDescending(e => e.MovedToDeadLetterAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<DateTime, int>> GetFailureTrendsAsync(
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-1);
        var entries = await _context.Set<DeadLetterQueueEntry>()
            .Where(e => e.MovedToDeadLetterAt >= since)
            .Select(e => e.MovedToDeadLetterAt)
            .ToListAsync(cancellationToken);

        return entries
            .GroupBy(d => new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetTopFailingJobTypesAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeadLetterQueueEntry>()
            .GroupBy(e => e.JobType)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetTopExceptionTypesAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var exceptionTypes = await _context.Set<DeadLetterQueueEntry>()
            .Where(e => e.ExceptionMessage != null)
            .Select(e => e.ExceptionMessage!.Split(new char[] { ':' }, StringSplitOptions.None)[0].Trim())
            .ToListAsync(cancellationToken);

        return exceptionTypes
            .GroupBy(e => e)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<bool> ExistsAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeadLetterQueueEntry>()
            .AnyAsync(e => e.JobId == jobId, cancellationToken);
    }

    public async Task<int> GetCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeadLetterQueueEntry>()
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetCountByQueueAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<DeadLetterQueueEntry>()
            .GroupBy(e => e.QueueName)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<byte[]> ExportToCsvAsync(
        DeadLetterQueueQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        query ??= new DeadLetterQueueQuery { PageSize = int.MaxValue };
        var result = await GetEntriesAsync(query, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Id,JobId,CorrelationId,JobType,MethodName,QueueName,RetryAttempts,ExceptionMessage,CreatedAt,FirstFailedAt,MovedToDeadLetterAt,IsRequeued,RequeuedAt,RequeuedBy");

        foreach (var entry in result.Entries)
        {
            csv.AppendLine($"{entry.Id},{entry.JobId},{entry.CorrelationId},{entry.JobType},{entry.MethodName},{entry.QueueName},{entry.RetryAttempts},\"{entry.ExceptionMessage?.Replace("\"", "\"\"")}\",{entry.CreatedAt:yyyy-MM-dd HH:mm:ss},{entry.FirstFailedAt:yyyy-MM-dd HH:mm:ss},{entry.MovedToDeadLetterAt:yyyy-MM-dd HH:mm:ss},{entry.IsRequeued},{entry.RequeuedAt?.ToString("yyyy-MM-dd HH:mm:ss")},{entry.RequeuedBy}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<int> BulkRequeueAsync(
        string? queueName = null,
        string? jobType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string requeuedBy = "System",
        string? newQueueName = null,
        CancellationToken cancellationToken = default)
    {
        var query = new DeadLetterQueueQuery
        {
            QueueName = queueName,
            JobType = jobType,
            FromDate = fromDate,
            ToDate = toDate,
            IncludeRequeued = false,
            PageSize = int.MaxValue
        };

        var result = await GetEntriesAsync(query, cancellationToken);
        var ids = result.Entries.Select(e => e.Id);

        return await RequeueEntriesAsync(ids, requeuedBy, newQueueName, cancellationToken);
    }

    public async Task<int> BulkDeleteAsync(
        string? queueName = null,
        string? jobType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool includeRequeued = false,
        CancellationToken cancellationToken = default)
    {
        var query = new DeadLetterQueueQuery
        {
            QueueName = queueName,
            JobType = jobType,
            FromDate = fromDate,
            ToDate = toDate,
            IncludeRequeued = includeRequeued,
            PageSize = int.MaxValue
        };

        var result = await GetEntriesAsync(query, cancellationToken);
        var ids = result.Entries.Select(e => e.Id);

        return await DeleteEntriesAsync(ids, cancellationToken);
    }

    /// <summary>
    /// Placeholder method for requeued jobs
    /// </summary>
    public static void RequeuedJobPlaceholder(string jobType, string methodName, object[]? args)
    {
        // This is a placeholder that will be replaced with actual job execution
        throw new NotImplementedException($"Requeued job placeholder: {jobType}.{methodName}");
    }
}

using FlightBooking.Infrastructure.BackgroundJobs.DeadLetterQueue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Api.Controllers.Admin;

/// <summary>
/// Controller for managing dead letter queue entries
/// </summary>
[ApiController]
[Route("api/admin/dead-letter-queue")]
[Authorize(Policy = "Admin")]
public class DeadLetterQueueController : ControllerBase
{
    private readonly IDeadLetterQueueService _deadLetterQueueService;
    private readonly ILogger<DeadLetterQueueController> _logger;

    public DeadLetterQueueController(
        IDeadLetterQueueService deadLetterQueueService,
        ILogger<DeadLetterQueueController> logger)
    {
        _deadLetterQueueService = deadLetterQueueService ?? throw new ArgumentNullException(nameof(deadLetterQueueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get dead letter queue entries with pagination and filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DeadLetterQueueResult>> GetEntries(
        [FromQuery] DeadLetterQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _deadLetterQueueService.GetEntriesAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter queue entries");
            return StatusCode(500, "An error occurred while retrieving dead letter queue entries");
        }
    }

    /// <summary>
    /// Get a specific dead letter queue entry by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeadLetterQueueEntry>> GetEntry(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _deadLetterQueueService.GetEntryByIdAsync(id, cancellationToken);
            if (entry == null)
            {
                return NotFound($"Dead letter queue entry with ID {id} not found");
            }

            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter queue entry {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the dead letter queue entry");
        }
    }

    /// <summary>
    /// Get dead letter queue statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DeadLetterQueueStats>> GetStats(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _deadLetterQueueService.GetStatsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter queue statistics");
            return StatusCode(500, "An error occurred while retrieving dead letter queue statistics");
        }
    }

    /// <summary>
    /// Requeue a dead letter entry back to Hangfire
    /// </summary>
    [HttpPost("{id:guid}/requeue")]
    public async Task<ActionResult> RequeueEntry(
        Guid id,
        [FromBody] RequeueRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requester = User.Identity?.Name ?? "Unknown";
            var success = await _deadLetterQueueService.RequeueEntryAsync(
                id, requester, request.NewQueueName, cancellationToken);

            if (!success)
            {
                return BadRequest("Failed to requeue entry. Entry may not exist or may already be requeued.");
            }

            _logger.LogInformation("Dead letter entry {Id} requeued by {User}", id, requester);
            return Ok(new { Message = "Entry successfully requeued", Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requeuing dead letter entry {Id}", id);
            return StatusCode(500, "An error occurred while requeuing the entry");
        }
    }

    /// <summary>
    /// Requeue multiple dead letter entries
    /// </summary>
    [HttpPost("requeue-bulk")]
    public async Task<ActionResult> RequeueBulk(
        [FromBody] BulkRequeueRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requester = User.Identity?.Name ?? "Unknown";
            var requeuedCount = await _deadLetterQueueService.RequeueEntriesAsync(
                request.Ids, requester, request.NewQueueName, cancellationToken);

            _logger.LogInformation("Bulk requeue completed by {User}. {Count} entries requeued", requester, requeuedCount);
            return Ok(new { Message = $"Successfully requeued {requeuedCount} entries", Count = requeuedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk requeue operation");
            return StatusCode(500, "An error occurred during bulk requeue operation");
        }
    }

    /// <summary>
    /// Delete a dead letter queue entry
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteEntry(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _deadLetterQueueService.DeleteEntryAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound($"Dead letter queue entry with ID {id} not found");
            }

            _logger.LogInformation("Dead letter entry {Id} deleted by {User}", id, User.Identity?.Name);
            return Ok(new { Message = "Entry successfully deleted", Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dead letter entry {Id}", id);
            return StatusCode(500, "An error occurred while deleting the entry");
        }
    }

    /// <summary>
    /// Delete multiple dead letter queue entries
    /// </summary>
    [HttpDelete("bulk")]
    public async Task<ActionResult> DeleteBulk(
        [FromBody] BulkDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _deadLetterQueueService.DeleteEntriesAsync(request.Ids, cancellationToken);

            _logger.LogInformation("Bulk delete completed by {User}. {Count} entries deleted", User.Identity?.Name, deletedCount);
            return Ok(new { Message = $"Successfully deleted {deletedCount} entries", Count = deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk delete operation");
            return StatusCode(500, "An error occurred during bulk delete operation");
        }
    }

    /// <summary>
    /// Get recent failures (last 24 hours)
    /// </summary>
    [HttpGet("recent-failures")]
    public async Task<ActionResult<List<DeadLetterQueueEntry>>> GetRecentFailures(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _deadLetterQueueService.GetRecentFailuresAsync(limit, cancellationToken);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent failures");
            return StatusCode(500, "An error occurred while retrieving recent failures");
        }
    }

    /// <summary>
    /// Get failure trends by hour for the last 24 hours
    /// </summary>
    [HttpGet("failure-trends")]
    public async Task<ActionResult<Dictionary<DateTime, int>>> GetFailureTrends(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var trends = await _deadLetterQueueService.GetFailureTrendsAsync(cancellationToken);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failure trends");
            return StatusCode(500, "An error occurred while retrieving failure trends");
        }
    }

    /// <summary>
    /// Get top failing job types
    /// </summary>
    [HttpGet("top-failing-jobs")]
    public async Task<ActionResult<Dictionary<string, int>>> GetTopFailingJobTypes(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var topJobs = await _deadLetterQueueService.GetTopFailingJobTypesAsync(limit, cancellationToken);
            return Ok(topJobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top failing job types");
            return StatusCode(500, "An error occurred while retrieving top failing job types");
        }
    }

    /// <summary>
    /// Export dead letter queue entries to CSV
    /// </summary>
    [HttpGet("export")]
    public async Task<ActionResult> ExportToCsv(
        [FromQuery] DeadLetterQueueQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var csvData = await _deadLetterQueueService.ExportToCsvAsync(query, cancellationToken);
            var fileName = $"dead-letter-queue-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

            return File(csvData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dead letter queue to CSV");
            return StatusCode(500, "An error occurred while exporting data");
        }
    }

    /// <summary>
    /// Cleanup old dead letter queue entries
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult> CleanupOldEntries(
        [FromBody] CleanupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var retentionPeriod = TimeSpan.FromDays(request.RetentionDays);
            var deletedCount = await _deadLetterQueueService.CleanupOldEntriesAsync(retentionPeriod, cancellationToken);

            _logger.LogInformation("Cleanup completed by {User}. {Count} old entries deleted", User.Identity?.Name, deletedCount);
            return Ok(new { Message = $"Successfully cleaned up {deletedCount} old entries", Count = deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup operation");
            return StatusCode(500, "An error occurred during cleanup operation");
        }
    }
}

#region Request Models

/// <summary>
/// Request model for requeuing an entry
/// </summary>
public class RequeueRequest
{
    /// <summary>
    /// Optional new queue name (if different from original)
    /// </summary>
    public string? NewQueueName { get; set; }
}

/// <summary>
/// Request model for bulk requeue operation
/// </summary>
public class BulkRequeueRequest
{
    /// <summary>
    /// List of entry IDs to requeue
    /// </summary>
    public List<Guid> Ids { get; set; } = new();

    /// <summary>
    /// Optional new queue name for all entries
    /// </summary>
    public string? NewQueueName { get; set; }
}

/// <summary>
/// Request model for bulk delete operation
/// </summary>
public class BulkDeleteRequest
{
    /// <summary>
    /// List of entry IDs to delete
    /// </summary>
    public List<Guid> Ids { get; set; } = new();
}

/// <summary>
/// Request model for cleanup operation
/// </summary>
public class CleanupRequest
{
    /// <summary>
    /// Number of days to retain entries (older entries will be deleted)
    /// </summary>
    public int RetentionDays { get; set; } = 30;
}

#endregion

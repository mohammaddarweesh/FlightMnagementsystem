using FlightBooking.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.BackgroundJobs.Audit;

public class AuditOutboxProcessor
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditOutboxProcessor> _logger;

    public AuditOutboxProcessor(ApplicationDbContext context, ILogger<AuditOutboxProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ProcessOutboxEntriesAsync()
    {
        _logger.LogInformation("Starting audit outbox processing");

        try
        {
            var batchSize = 100;
            var processedCount = 0;
            var errorCount = 0;

            while (true)
            {
                // Get unprocessed entries that are ready for retry
                var outboxEntries = await _context.AuditOutbox
                    .Where(ao => !ao.IsProcessed && 
                                (ao.NextRetryAt == null || ao.NextRetryAt <= DateTime.UtcNow))
                    .OrderBy(ao => ao.CreatedAt)
                    .Take(batchSize)
                    .ToListAsync();

                if (!outboxEntries.Any())
                {
                    _logger.LogInformation("No more outbox entries to process");
                    break;
                }

                _logger.LogInformation("Processing {Count} outbox entries", outboxEntries.Count);

                foreach (var outboxEntry in outboxEntries)
                {
                    try
                    {
                        // Convert outbox entry to audit event
                        var auditEvent = outboxEntry.ToAuditEvent();
                        
                        // Add to audit events table
                        _context.AuditEvents.Add(auditEvent);
                        
                        // Mark outbox entry as processed
                        outboxEntry.MarkAsProcessed();
                        
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process outbox entry {Id}", outboxEntry.Id);
                        
                        // Mark as failed with retry
                        outboxEntry.MarkAsFailedWithRetry(ex.Message);
                        errorCount++;
                    }
                }

                // Save changes in batch
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Saved batch of {Count} entries", outboxEntries.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save batch of outbox entries");
                    
                    // Reset the context to avoid issues with subsequent batches
                    foreach (var entry in _context.ChangeTracker.Entries())
                    {
                        entry.State = EntityState.Detached;
                    }
                    
                    throw;
                }

                // If we processed less than the batch size, we're done
                if (outboxEntries.Count < batchSize)
                    break;
            }

            _logger.LogInformation("Audit outbox processing completed. Processed: {ProcessedCount}, Errors: {ErrorCount}", 
                processedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during audit outbox processing");
            throw;
        }
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
    public async Task CleanupOldEntriesAsync()
    {
        _logger.LogInformation("Starting audit outbox cleanup");

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Keep processed entries for 7 days
            var maxFailedAge = DateTime.UtcNow.AddDays(-30); // Keep failed entries for 30 days

            // Delete old processed entries
            var processedEntries = await _context.AuditOutbox
                .Where(ao => ao.IsProcessed && ao.ProcessedAt < cutoffDate)
                .ToListAsync();

            if (processedEntries.Any())
            {
                _context.AuditOutbox.RemoveRange(processedEntries);
                _logger.LogInformation("Removing {Count} old processed outbox entries", processedEntries.Count);
            }

            // Delete very old failed entries that exceeded max retry count
            var oldFailedEntries = await _context.AuditOutbox
                .Where(ao => !ao.IsProcessed && 
                            ao.RetryCount >= Domain.Audit.AuditOutbox.MaxRetryCount && 
                            ao.CreatedAt < maxFailedAge)
                .ToListAsync();

            if (oldFailedEntries.Any())
            {
                _context.AuditOutbox.RemoveRange(oldFailedEntries);
                _logger.LogInformation("Removing {Count} old failed outbox entries", oldFailedEntries.Count);
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Audit outbox cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audit outbox cleanup");
            throw;
        }
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
    public async Task ArchiveOldAuditEventsAsync()
    {
        _logger.LogInformation("Starting audit events archival");

        try
        {
            var archiveAfterDays = 365; // Archive events older than 1 year
            var cutoffDate = DateTime.UtcNow.AddDays(-archiveAfterDays);

            // Count old events
            var oldEventsCount = await _context.AuditEvents
                .Where(ae => ae.CreatedAt < cutoffDate)
                .CountAsync();

            if (oldEventsCount == 0)
            {
                _logger.LogInformation("No old audit events to archive");
                return;
            }

            _logger.LogInformation("Found {Count} audit events older than {Days} days for archival", 
                oldEventsCount, archiveAfterDays);

            // In a real implementation, you might:
            // 1. Export to cold storage (S3, Azure Blob, etc.)
            // 2. Compress the data
            // 3. Move to a separate archive database
            // 4. Create summary records

            // For now, we'll just log the count and could implement actual archival later
            _logger.LogInformation("Audit events archival completed (archival logic to be implemented based on requirements)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audit events archival");
            throw;
        }
    }
}

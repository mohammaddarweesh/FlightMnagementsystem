using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlightBooking.BackgroundJobs.Audit;

public class AuditJobScheduler : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditJobScheduler> _logger;

    public AuditJobScheduler(IServiceProvider serviceProvider, ILogger<AuditJobScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting audit job scheduler");

        try
        {
            // Schedule recurring jobs
            ScheduleAuditJobs();
            
            _logger.LogInformation("Audit jobs scheduled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule audit jobs");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping audit job scheduler");
        return Task.CompletedTask;
    }

    private void ScheduleAuditJobs()
    {
        // Process outbox entries every minute
        RecurringJob.AddOrUpdate<AuditOutboxProcessor>(
            "audit-outbox-processor",
            processor => processor.ProcessOutboxEntriesAsync(),
            "*/1 * * * *", // Every minute
            TimeZoneInfo.Utc);

        // Cleanup old outbox entries daily at 2 AM
        RecurringJob.AddOrUpdate<AuditOutboxProcessor>(
            "audit-outbox-cleanup",
            processor => processor.CleanupOldEntriesAsync(),
            "0 2 * * *", // Daily at 2 AM UTC
            TimeZoneInfo.Utc);

        // Archive old audit events weekly on Sunday at 3 AM
        RecurringJob.AddOrUpdate<AuditOutboxProcessor>(
            "audit-events-archival",
            processor => processor.ArchiveOldAuditEventsAsync(),
            "0 3 * * 0", // Weekly on Sunday at 3 AM UTC
            TimeZoneInfo.Utc);

        _logger.LogInformation("Scheduled audit jobs:");
        _logger.LogInformation("- audit-outbox-processor: Every minute");
        _logger.LogInformation("- audit-outbox-cleanup: Daily at 2 AM UTC");
        _logger.LogInformation("- audit-events-archival: Weekly on Sunday at 3 AM UTC");
    }
}

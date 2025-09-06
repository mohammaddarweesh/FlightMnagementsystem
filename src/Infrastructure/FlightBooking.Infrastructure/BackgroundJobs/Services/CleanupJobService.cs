using FlightBooking.Infrastructure.BackgroundJobs.Attributes;
using FlightBooking.Infrastructure.BackgroundJobs.Configuration;
using FlightBooking.Infrastructure.Data;
using FlightBooking.Domain.Bookings;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.BackgroundJobs.Services;

public class CleanupJobService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CleanupJobService> _logger;

    public CleanupJobService(ApplicationDbContext context, ILogger<CleanupJobService> logger)
    {
        _context = context;
        _logger = logger;
    }

    [CleanupRetry]
    [Queue(HangfireQueues.Cleanup)]
    public async Task CleanupExpiredBookingsAsync(string? correlationId = null)
    {
        _logger.LogInformation("Starting cleanup of expired bookings");
        
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var expiredBookings = await _context.Bookings
            .Where(b => b.Status == BookingStatus.Expired && b.CreatedAt < cutoffDate)
            .ToListAsync();

        foreach (var booking in expiredBookings)
        {
            booking.IsArchived = true;
            booking.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleaned up {Count} expired bookings", expiredBookings.Count);
    }

    [CleanupRetry]
    [Queue(HangfireQueues.Cleanup)]
    public async Task SystemCleanupAsync(string? correlationId = null)
    {
        _logger.LogInformation("Starting system cleanup");
        
        var auditCutoffDate = DateTime.UtcNow.AddYears(-1);
        var oldAuditEvents = await _context.AuditLogs
            .Where(a => a.CreatedAt < auditCutoffDate)
            .ToListAsync();

        _context.AuditLogs.RemoveRange(oldAuditEvents);
        await _context.SaveChangesAsync();

        _logger.LogInformation("System cleanup completed, removed {Count} old audit events", oldAuditEvents.Count);
    }

    [CleanupRetry]
    [Queue(HangfireQueues.Cleanup)]
    public async Task DatabaseMaintenanceAsync(string? correlationId = null)
    {
        _logger.LogInformation("Starting database maintenance");
        // Database maintenance logic would go here
        _logger.LogInformation("Database maintenance completed");
    }
}

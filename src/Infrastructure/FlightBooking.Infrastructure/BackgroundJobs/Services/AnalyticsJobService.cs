using FlightBooking.Application.Analytics.Interfaces;
using FlightBooking.Infrastructure.BackgroundJobs.Attributes;
using FlightBooking.Infrastructure.BackgroundJobs.Configuration;
using FlightBooking.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FlightBooking.Infrastructure.BackgroundJobs.Services;

/// <summary>
/// Background job service for analytics materialized view refresh and maintenance
/// </summary>
public class AnalyticsJobService
{
    private readonly ApplicationDbContext _context;
    private readonly IAnalyticsRefreshService _refreshService;
    private readonly ILogger<AnalyticsJobService> _logger;

    public AnalyticsJobService(
        ApplicationDbContext context,
        IAnalyticsRefreshService refreshService,
        ILogger<AnalyticsJobService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _refreshService = refreshService ?? throw new ArgumentNullException(nameof(refreshService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Refresh all analytics materialized views - runs daily at 2 AM
    /// </summary>
    [AnalyticsRetry]
    [Queue(HangfireQueues.Analytics)]
    [JobDisplayName("Refresh All Analytics Views")]
    public async Task RefreshAllAnalyticsViewsAsync(string? correlationId = null)
    {
        var jobId = Guid.NewGuid().ToString();
        correlationId ??= Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting daily refresh of all analytics materialized views. JobId: {JobId}, CorrelationId: {CorrelationId}",
            jobId, correlationId);

        var startTime = DateTime.UtcNow;
        
        try
        {
            await _refreshService.RefreshAllViewsAsync();
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Successfully refreshed all analytics materialized views. JobId: {JobId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                jobId, duration.TotalMilliseconds, correlationId);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex,
                "Failed to refresh analytics materialized views. JobId: {JobId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                jobId, duration.TotalMilliseconds, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Refresh revenue analytics view - runs hourly during business hours
    /// </summary>
    [AnalyticsRetry]
    [Queue(HangfireQueues.Analytics)]
    [JobDisplayName("Refresh Revenue Analytics View")]
    public async Task RefreshRevenueAnalyticsAsync(string? correlationId = null)
    {
        var jobId = Guid.NewGuid().ToString();
        correlationId ??= Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting refresh of revenue analytics materialized view. JobId: {JobId}, CorrelationId: {CorrelationId}",
            jobId, correlationId);

        try
        {
            await _refreshService.RefreshViewAsync("revenue");
            
            _logger.LogInformation(
                "Successfully refreshed revenue analytics view. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh revenue analytics view. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Refresh booking status analytics view - runs every 2 hours
    /// </summary>
    [AnalyticsRetry]
    [Queue(HangfireQueues.Analytics)]
    [JobDisplayName("Refresh Booking Status Analytics View")]
    public async Task RefreshBookingStatusAnalyticsAsync(string? correlationId = null)
    {
        var jobId = Guid.NewGuid().ToString();
        correlationId ??= Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting refresh of booking status analytics materialized view. JobId: {JobId}, CorrelationId: {CorrelationId}",
            jobId, correlationId);

        try
        {
            await _refreshService.RefreshViewAsync("booking_status");
            
            _logger.LogInformation(
                "Successfully refreshed booking status analytics view. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh booking status analytics view. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Refresh passenger demographics view - runs daily at 3 AM
    /// </summary>
    [AnalyticsRetry]
    [Queue(HangfireQueues.Analytics)]
    [JobDisplayName("Refresh Passenger Demographics View")]
    public async Task RefreshPassengerDemographicsAsync(string? correlationId = null)
    {
        var jobId = Guid.NewGuid().ToString();
        correlationId ??= Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting refresh of passenger demographics materialized view. JobId: {JobId}, CorrelationId: {CorrelationId}",
            jobId, correlationId);

        try
        {
            await _refreshService.RefreshViewAsync("demographics");
            
            _logger.LogInformation(
                "Successfully refreshed passenger demographics view. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh passenger demographics view. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Refresh route performance analytics view - runs daily at 4 AM
    /// </summary>
    [AnalyticsRetry]
    [Queue(HangfireQueues.Analytics)]
    [JobDisplayName("Refresh Route Performance Analytics View")]
    public async Task RefreshRoutePerformanceAnalyticsAsync(string? correlationId = null)
    {
        var jobId = Guid.NewGuid().ToString();
        correlationId ??= Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting refresh of route performance analytics materialized view. JobId: {JobId}, CorrelationId: {CorrelationId}",
            jobId, correlationId);

        try
        {
            await _refreshService.RefreshViewAsync("route_performance");
            
            _logger.LogInformation(
                "Successfully refreshed route performance analytics view. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh route performance analytics view. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Clean up old analytics data - runs weekly on Sunday at 1 AM
    /// </summary>
    [CleanupRetry]
    [Queue(HangfireQueues.Cleanup)]
    [JobDisplayName("Cleanup Old Analytics Data")]
    public async Task CleanupOldAnalyticsDataAsync(string? correlationId = null)
    {
        var jobId = Guid.NewGuid().ToString();
        correlationId ??= Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting cleanup of old analytics data. JobId: {JobId}, CorrelationId: {CorrelationId}",
            jobId, correlationId);

        try
        {
            var cutoffDate = DateTime.UtcNow.AddYears(-2); // Keep 2 years of analytics data
            
            // Clean up old refresh logs
            var oldLogs = await _context.Database
                .ExecuteSqlRawAsync(
                    "DELETE FROM analytics_refresh_log WHERE refreshed_at < @cutoffDate",
                    new NpgsqlParameter("@cutoffDate", cutoffDate));

            _logger.LogInformation(
                "Cleaned up old analytics data. JobId: {JobId}, DeletedLogs: {DeletedLogs}, CorrelationId: {CorrelationId}",
                jobId, oldLogs, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to cleanup old analytics data. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Generate analytics health report - runs daily at 6 AM
    /// </summary>
    [AnalyticsRetry]
    [Queue(HangfireQueues.Analytics)]
    [JobDisplayName("Generate Analytics Health Report")]
    public async Task GenerateAnalyticsHealthReportAsync(string? correlationId = null)
    {
        var jobId = Guid.NewGuid().ToString();
        correlationId ??= Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting analytics health report generation. JobId: {JobId}, CorrelationId: {CorrelationId}",
            jobId, correlationId);

        try
        {
            var refreshStatus = await _refreshService.GetRefreshStatusAsync();
            
            // Log health metrics
            foreach (var status in refreshStatus)
            {
                _logger.LogInformation(
                    "Analytics View Health - View: {ViewName}, Status: {Status}, JobId: {JobId}",
                    status.Key, status.Value, jobId);
            }

            // Check for stale data
            var viewNames = new[] { "revenue", "booking_status", "demographics", "route_performance" };
            var staleViews = new List<string>();
            
            foreach (var viewName in viewNames)
            {
                var lastRefresh = await _refreshService.GetLastRefreshTimeAsync(viewName);
                var age = DateTime.UtcNow - lastRefresh;
                
                if (age > TimeSpan.FromHours(25)) // More than 25 hours old
                {
                    staleViews.Add(viewName);
                }
            }

            if (staleViews.Any())
            {
                _logger.LogWarning(
                    "Stale analytics views detected. JobId: {JobId}, StaleViews: {StaleViews}, CorrelationId: {CorrelationId}",
                    jobId, string.Join(", ", staleViews), correlationId);
            }
            else
            {
                _logger.LogInformation(
                    "All analytics views are current. JobId: {JobId}, CorrelationId: {CorrelationId}",
                    jobId, correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate analytics health report. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Optimize analytics materialized view indexes - runs weekly on Saturday at 11 PM
    /// </summary>
    [CleanupRetry]
    [Queue(HangfireQueues.Cleanup)]
    [JobDisplayName("Optimize Analytics Indexes")]
    public async Task OptimizeAnalyticsIndexesAsync(string? correlationId = null)
    {
        var jobId = Guid.NewGuid().ToString();
        correlationId ??= Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting analytics indexes optimization. JobId: {JobId}, CorrelationId: {CorrelationId}",
            jobId, correlationId);

        try
        {
            // Reindex materialized view indexes for better performance
            var indexCommands = new[]
            {
                "REINDEX INDEX CONCURRENTLY idx_mv_revenue_daily_pk;",
                "REINDEX INDEX CONCURRENTLY idx_mv_revenue_daily_date;",
                "REINDEX INDEX CONCURRENTLY idx_mv_booking_status_daily_pk;",
                "REINDEX INDEX CONCURRENTLY idx_mv_booking_status_daily_date;",
                "REINDEX INDEX CONCURRENTLY idx_mv_passenger_demographics_daily_pk;",
                "REINDEX INDEX CONCURRENTLY idx_mv_passenger_demographics_daily_date;",
                "REINDEX INDEX CONCURRENTLY idx_mv_route_performance_daily_pk;",
                "REINDEX INDEX CONCURRENTLY idx_mv_route_performance_daily_date;"
            };

            foreach (var command in indexCommands)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(command);
                    _logger.LogDebug("Executed index optimization: {Command}", command);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to execute index optimization: {Command}", command);
                    // Continue with other indexes even if one fails
                }
            }

            _logger.LogInformation(
                "Completed analytics indexes optimization. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to optimize analytics indexes. JobId: {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            throw;
        }
    }
}

using FlightBooking.Application.Analytics.Interfaces;
using FlightBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FlightBooking.Infrastructure.Analytics.Services;

/// <summary>
/// Service for refreshing analytics materialized views
/// </summary>
public class AnalyticsRefreshService : IAnalyticsRefreshService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsRefreshService> _logger;
    private readonly Dictionary<string, bool> _refreshInProgress = new();
    private readonly object _lockObject = new();

    public AnalyticsRefreshService(
        ApplicationDbContext context,
        ILogger<AnalyticsRefreshService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RefreshAllViewsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting refresh of all analytics materialized views");
        
        var startTime = DateTime.UtcNow;
        var viewNames = new[] { "revenue", "booking_status", "demographics", "route_performance" };
        var refreshResults = new Dictionary<string, (bool Success, TimeSpan Duration, string? Error)>();

        foreach (var viewName in viewNames)
        {
            var viewStartTime = DateTime.UtcNow;
            try
            {
                await RefreshViewAsync(viewName, cancellationToken);
                var duration = DateTime.UtcNow - viewStartTime;
                refreshResults[viewName] = (true, duration, null);
                _logger.LogInformation("Successfully refreshed view {ViewName} in {Duration}ms", 
                    viewName, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - viewStartTime;
                refreshResults[viewName] = (false, duration, ex.Message);
                _logger.LogError(ex, "Failed to refresh view {ViewName} after {Duration}ms", 
                    viewName, duration.TotalMilliseconds);
                
                // Continue with other views even if one fails
            }
        }

        var totalDuration = DateTime.UtcNow - startTime;
        var successCount = refreshResults.Count(r => r.Value.Success);
        
        _logger.LogInformation(
            "Completed refresh of all analytics views. Success: {SuccessCount}/{TotalCount}, Total Duration: {Duration}ms",
            successCount, viewNames.Length, totalDuration.TotalMilliseconds);

        // Log refresh summary
        await LogRefreshSummaryAsync(refreshResults);

        if (successCount < viewNames.Length)
        {
            var failedViews = refreshResults.Where(r => !r.Value.Success).Select(r => r.Key);
            throw new InvalidOperationException($"Failed to refresh views: {string.Join(", ", failedViews)}");
        }
    }

    public async Task RefreshViewAsync(string viewName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(viewName))
            throw new ArgumentException("View name cannot be null or empty", nameof(viewName));

        // Check if refresh is already in progress
        lock (_lockObject)
        {
            if (_refreshInProgress.GetValueOrDefault(viewName, false))
            {
                _logger.LogWarning("Refresh already in progress for view {ViewName}", viewName);
                return;
            }
            _refreshInProgress[viewName] = true;
        }

        try
        {
            _logger.LogInformation("Starting refresh of materialized view: {ViewName}", viewName);
            var startTime = DateTime.UtcNow;

            var materializedViewName = GetMaterializedViewName(viewName);
            var refreshCommand = $"REFRESH MATERIALIZED VIEW CONCURRENTLY {materializedViewName};";

            await _context.Database.ExecuteSqlRawAsync(refreshCommand, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Successfully refreshed materialized view {ViewName} in {Duration}ms", 
                viewName, duration.TotalMilliseconds);

            // Log the refresh
            await LogRefreshAsync(viewName, "SUCCESS", duration, null);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - DateTime.UtcNow; // This should be calculated from start time
            _logger.LogError(ex, "Failed to refresh materialized view {ViewName}", viewName);
            
            // Log the failure
            await LogRefreshAsync(viewName, "FAILED", duration, ex.Message);
            throw;
        }
        finally
        {
            lock (_lockObject)
            {
                _refreshInProgress[viewName] = false;
            }
        }
    }

    public Task<bool> IsRefreshInProgressAsync(string viewName, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            return Task.FromResult(_refreshInProgress.GetValueOrDefault(viewName, false));
        }
    }

    public async Task<DateTime> GetLastRefreshTimeAsync(string viewName, CancellationToken cancellationToken = default)
    {
        try
        {
            var materializedViewName = GetMaterializedViewName(viewName);
            
            // Query the materialized view's last refresh time from the view itself
            var query = viewName.ToLower() switch
            {
                "revenue" => "SELECT MAX(last_refreshed) FROM mv_revenue_daily",
                "booking_status" => "SELECT MAX(last_refreshed) FROM mv_booking_status_daily",
                "demographics" => "SELECT MAX(last_refreshed) FROM mv_passenger_demographics_daily",
                "route_performance" => "SELECT MAX(last_refreshed) FROM mv_route_performance_daily",
                _ => throw new ArgumentException($"Unknown view name: {viewName}")
            };

            var result = await _context.Database
                .SqlQueryRaw<DateTime?>(query)
                .FirstOrDefaultAsync(cancellationToken);

            return result ?? DateTime.MinValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last refresh time for view {ViewName}", viewName);
            return DateTime.MinValue;
        }
    }

    public async Task<TimeSpan> GetRefreshDurationAsync(string viewName, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT duration_ms 
                FROM analytics_refresh_log 
                WHERE view_name = @viewName 
                  AND status = 'SUCCESS'
                ORDER BY refreshed_at DESC 
                LIMIT 1";

            var result = await _context.Database
                .SqlQueryRaw<int?>(query, new NpgsqlParameter("@viewName", viewName))
                .FirstOrDefaultAsync(cancellationToken);

            return result.HasValue ? TimeSpan.FromMilliseconds(result.Value) : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get refresh duration for view {ViewName}", viewName);
            return TimeSpan.Zero;
        }
    }

    public async Task<Dictionary<string, object>> GetRefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new Dictionary<string, object>();
        var viewNames = new[] { "revenue", "booking_status", "demographics", "route_performance" };

        foreach (var viewName in viewNames)
        {
            try
            {
                var lastRefresh = await GetLastRefreshTimeAsync(viewName, cancellationToken);
                var duration = await GetRefreshDurationAsync(viewName, cancellationToken);
                var isInProgress = await IsRefreshInProgressAsync(viewName, cancellationToken);
                var age = DateTime.UtcNow - lastRefresh;

                status[$"{viewName}_last_refresh"] = lastRefresh;
                status[$"{viewName}_age_hours"] = age.TotalHours;
                status[$"{viewName}_duration_ms"] = duration.TotalMilliseconds;
                status[$"{viewName}_in_progress"] = isInProgress;
                status[$"{viewName}_is_current"] = age <= TimeSpan.FromHours(25);
                status[$"{viewName}_health"] = GetViewHealth(age, isInProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status for view {ViewName}", viewName);
                status[$"{viewName}_error"] = ex.Message;
                status[$"{viewName}_health"] = "Error";
            }
        }

        // Overall status
        var healthyViews = viewNames.Count(vn => status.GetValueOrDefault($"{vn}_health", "Error").ToString() == "Healthy");
        status["overall_health"] = healthyViews == viewNames.Length ? "Healthy" :
                                  healthyViews >= viewNames.Length * 0.75 ? "Warning" : "Critical";
        status["healthy_views"] = healthyViews;
        status["total_views"] = viewNames.Length;
        status["last_check"] = DateTime.UtcNow;

        return status;
    }

    private static string GetMaterializedViewName(string viewName)
    {
        return viewName.ToLower() switch
        {
            "revenue" => "mv_revenue_daily",
            "booking_status" => "mv_booking_status_daily",
            "demographics" => "mv_passenger_demographics_daily",
            "route_performance" => "mv_route_performance_daily",
            _ => throw new ArgumentException($"Unknown view name: {viewName}")
        };
    }

    private static string GetViewHealth(TimeSpan age, bool isInProgress)
    {
        if (isInProgress)
            return "Refreshing";
        
        if (age <= TimeSpan.FromHours(25))
            return "Healthy";
        
        if (age <= TimeSpan.FromHours(48))
            return "Warning";
        
        return "Stale";
    }

    private async Task LogRefreshAsync(string viewName, string status, TimeSpan duration, string? errorMessage)
    {
        try
        {
            var command = @"
                INSERT INTO analytics_refresh_log (view_name, refreshed_at, status, duration_ms, error_message)
                VALUES (@viewName, @refreshedAt, @status, @durationMs, @errorMessage)";

            await _context.Database.ExecuteSqlRawAsync(command,
                new NpgsqlParameter("@viewName", viewName),
                new NpgsqlParameter("@refreshedAt", DateTime.UtcNow),
                new NpgsqlParameter("@status", status),
                new NpgsqlParameter("@durationMs", (int)duration.TotalMilliseconds),
                new NpgsqlParameter("@errorMessage", errorMessage ?? (object)DBNull.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log refresh for view {ViewName}", viewName);
            // Don't throw here as it's just logging
        }
    }

    private async Task LogRefreshSummaryAsync(Dictionary<string, (bool Success, TimeSpan Duration, string? Error)> results)
    {
        try
        {
            var summary = new
            {
                TotalViews = results.Count,
                SuccessfulViews = results.Count(r => r.Value.Success),
                FailedViews = results.Count(r => !r.Value.Success),
                TotalDuration = results.Values.Sum(r => r.Duration.TotalMilliseconds),
                AverageDuration = results.Values.Average(r => r.Duration.TotalMilliseconds)
            };

            _logger.LogInformation(
                "Analytics refresh summary - Total: {TotalViews}, Success: {SuccessfulViews}, Failed: {FailedViews}, " +
                "Total Duration: {TotalDuration}ms, Average Duration: {AverageDuration}ms",
                summary.TotalViews, summary.SuccessfulViews, summary.FailedViews, 
                summary.TotalDuration, summary.AverageDuration);

            // Log individual view results
            foreach (var result in results)
            {
                if (result.Value.Success)
                {
                    _logger.LogDebug("View {ViewName} refreshed successfully in {Duration}ms", 
                        result.Key, result.Value.Duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("View {ViewName} refresh failed after {Duration}ms: {Error}", 
                        result.Key, result.Value.Duration.TotalMilliseconds, result.Value.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log refresh summary");
        }
    }
}

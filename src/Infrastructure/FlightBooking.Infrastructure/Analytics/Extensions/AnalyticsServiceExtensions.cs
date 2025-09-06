using FlightBooking.Application.Analytics.Interfaces;
using FlightBooking.Infrastructure.Analytics.Data;
using FlightBooking.Infrastructure.Analytics.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Analytics.Extensions;

/// <summary>
/// Extension methods for registering analytics services
/// </summary>
public static class AnalyticsServiceExtensions
{
    /// <summary>
    /// Add analytics services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Analytics DbContext
        services.AddDbContext<AnalyticsDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString);
        });

        // Register analytics services
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<IAnalyticsRefreshService, AnalyticsRefreshService>();
        
        // Register cache service based on configuration
        var cacheProvider = configuration.GetValue<string>("Analytics:CacheProvider", "Memory");

        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            // Check if distributed cache is already registered
            var distributedCacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
            if (distributedCacheDescriptor != null)
            {
                services.AddScoped<IAnalyticsCacheService, AnalyticsCacheService>();
            }
            else
            {
                // Fallback to in-memory cache if Redis is not configured
                services.AddSingleton<IAnalyticsCacheService, InMemoryAnalyticsCacheService>();
            }
        }
        else
        {
            // Default to in-memory cache
            services.AddSingleton<IAnalyticsCacheService, InMemoryAnalyticsCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Add analytics health checks
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddAnalyticsHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck<AnalyticsHealthCheck>("analytics_materialized_views")
            .AddCheck<AnalyticsDataQualityHealthCheck>("analytics_data_quality");

        return services;
    }

    /// <summary>
    /// Configure analytics background jobs
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddAnalyticsBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        // Analytics job service is already registered in HangfireServiceExtensions
        // This method can be used for additional analytics-specific job configuration
        
        return services;
    }
}

/// <summary>
/// Health check for analytics materialized views
/// </summary>
public class AnalyticsHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IAnalyticsRefreshService _refreshService;
    private readonly ILogger<AnalyticsHealthCheck> _logger;

    public AnalyticsHealthCheck(IAnalyticsRefreshService refreshService, ILogger<AnalyticsHealthCheck> logger)
    {
        _refreshService = refreshService;
        _logger = logger;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _refreshService.GetRefreshStatusAsync(cancellationToken);
            var healthyViews = 0;
            var totalViews = 0;
            var issues = new List<string>();

            var viewNames = new[] { "revenue", "booking_status", "demographics", "route_performance" };
            
            foreach (var viewName in viewNames)
            {
                totalViews++;
                var health = status.GetValueOrDefault($"{viewName}_health", "Unknown").ToString();
                var isCurrent = (bool)status.GetValueOrDefault($"{viewName}_is_current", false);
                
                if (health == "Healthy" && isCurrent)
                {
                    healthyViews++;
                }
                else
                {
                    issues.Add($"{viewName}: {health}");
                }
            }

            var healthPercentage = (double)healthyViews / totalViews;
            
            if (healthPercentage >= 1.0)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    $"All {totalViews} analytics views are healthy");
            }
            else if (healthPercentage >= 0.75)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                    $"{healthyViews}/{totalViews} analytics views are healthy. Issues: {string.Join(", ", issues)}");
            }
            else
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    $"Only {healthyViews}/{totalViews} analytics views are healthy. Issues: {string.Join(", ", issues)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check analytics health");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Failed to check analytics health", ex);
        }
    }
}

/// <summary>
/// Health check for analytics data quality
/// </summary>
public class AnalyticsDataQualityHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsDataQualityHealthCheck> _logger;

    public AnalyticsDataQualityHealthCheck(IAnalyticsService analyticsService, ILogger<AnalyticsDataQualityHealthCheck> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _analyticsService.GetDataQualityMetricsAsync(cancellationToken);
            var qualityScore = (decimal)metrics.GetValueOrDefault("overall_quality_score", 0m);
            var qualityGrade = metrics.GetValueOrDefault("data_quality_grade", "Unknown").ToString();

            var data = new Dictionary<string, object>
            {
                ["quality_score"] = qualityScore,
                ["quality_grade"] = qualityGrade!,
                ["last_check"] = DateTime.UtcNow
            };

            if (qualityScore >= 90)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    $"Analytics data quality is {qualityGrade} ({qualityScore}%)", data);
            }
            else if (qualityScore >= 75)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                    $"Analytics data quality is {qualityGrade} ({qualityScore}%)", null, data);
            }
            else
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    $"Analytics data quality is {qualityGrade} ({qualityScore}%)", null, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check analytics data quality");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Failed to check analytics data quality", ex);
        }
    }
}

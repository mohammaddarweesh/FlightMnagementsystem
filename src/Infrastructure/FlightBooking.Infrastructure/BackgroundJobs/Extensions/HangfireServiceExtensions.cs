using FlightBooking.Infrastructure.BackgroundJobs.Configuration;
using FlightBooking.Infrastructure.BackgroundJobs.DeadLetterQueue;
using FlightBooking.Infrastructure.BackgroundJobs.Services;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightBooking.Infrastructure.BackgroundJobs.Extensions;

/// <summary>
/// Extension methods for configuring Hangfire services
/// </summary>
public static class HangfireServiceExtensions
{
    /// <summary>
    /// Add Hangfire services to the service collection
    /// </summary>
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Hangfire settings
        services.Configure<HangfireConfiguration>(
            configuration.GetSection(HangfireConfiguration.SectionName));

        var hangfireConfig = configuration
            .GetSection(HangfireConfiguration.SectionName)
            .Get<HangfireConfiguration>() ?? new HangfireConfiguration();

        // Add Hangfire services
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(hangfireConfig.ConnectionString, new PostgreSqlStorageOptions
                {
                    SchemaName = hangfireConfig.SchemaName
                })
                .UseFilter(new AutomaticRetryAttribute { Attempts = 0 }) // Disable default retry
                .UseDashboardMetric(DashboardMetrics.ServerCount)
                .UseDashboardMetric(DashboardMetrics.RecurringJobCount)
                .UseDashboardMetric(DashboardMetrics.RetriesCount)
                .UseDashboardMetric(DashboardMetrics.EnqueuedCountOrNull)
                .UseDashboardMetric(DashboardMetrics.FailedCountOrNull)
                .UseDashboardMetric(DashboardMetrics.EnqueuedAndQueueCount)
                .UseDashboardMetric(DashboardMetrics.ScheduledCount)
                .UseDashboardMetric(DashboardMetrics.ProcessingCount)
                .UseDashboardMetric(DashboardMetrics.SucceededCount)
                .UseDashboardMetric(DashboardMetrics.FailedCount)
                .UseDashboardMetric(DashboardMetrics.DeletedCount)
                .UseDashboardMetric(DashboardMetrics.AwaitingCount);
        });

        // Add Hangfire server
        services.AddHangfireServer(options =>
        {
            options.ServerName = hangfireConfig.Server.ServerName;
            options.WorkerCount = hangfireConfig.Server.WorkerCount;
            options.Queues = hangfireConfig.Server.Queues;
            options.ShutdownTimeout = hangfireConfig.Server.ShutdownTimeout;
            options.HeartbeatInterval = hangfireConfig.Server.HeartbeatInterval;
            options.ServerCheckInterval = hangfireConfig.Server.ServerCheckInterval;
        });

        // Register background job services
        services.AddScoped<EmailJobService>();
        services.AddScoped<ReportJobService>();
        services.AddScoped<CleanupJobService>();
        services.AddScoped<PricingJobService>();
        services.AddScoped<OutboxJobService>();
        services.AddScoped<AnalyticsJobService>();

        // Register dead letter queue service
        services.AddScoped<IDeadLetterQueueService, DeadLetterQueueService>();

        return services;
    }

    /// <summary>
    /// Configure Hangfire dashboard and middleware
    /// </summary>
    public static IApplicationBuilder UseHangfireDashboard(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var hangfireConfig = configuration
            .GetSection(HangfireConfiguration.SectionName)
            .Get<HangfireConfiguration>() ?? new HangfireConfiguration();

        if (!hangfireConfig.Dashboard.Enabled)
            return app;

        var dashboardOptions = new DashboardOptions
        {
            AppPath = "/",
            DashboardTitle = hangfireConfig.Dashboard.Title,
            StatsPollingInterval = 2000,
            DisplayStorageConnectionString = false,
            DarkModeEnabled = hangfireConfig.Dashboard.DarkTheme,
            Authorization = hangfireConfig.Dashboard.RequireAuthentication
                ? new[] { new HangfireAuthorizationFilter(hangfireConfig.Dashboard.RequiredPolicy) }
                : new IDashboardAuthorizationFilter[0]
        };

        app.UseHangfireDashboard(hangfireConfig.Dashboard.Path, dashboardOptions);

        return app;
    }

    /// <summary>
    /// Configure recurring jobs
    /// </summary>
    public static IApplicationBuilder ConfigureRecurringJobs(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var hangfireConfig = configuration
            .GetSection(HangfireConfiguration.SectionName)
            .Get<HangfireConfiguration>() ?? new HangfireConfiguration();

        // Nightly revenue report
        RecurringJob.AddOrUpdate<ReportJobService>(
            "nightly-revenue-report",
            service => service.GenerateNightlyRevenueReportAsync(DateTime.UtcNow.Date, null),
            "0 2 * * *", // 2 AM daily
            TimeZoneInfo.Utc,
            HangfireQueues.Reports);

        // Weekly booking summary
        RecurringJob.AddOrUpdate<ReportJobService>(
            "weekly-booking-summary",
            service => service.GenerateWeeklyBookingSummaryAsync(DateTime.UtcNow.AddDays(-7).Date, null),
            "0 3 * * 1", // 3 AM every Monday
            TimeZoneInfo.Utc,
            HangfireQueues.Reports);

        // Monthly performance report
        RecurringJob.AddOrUpdate<ReportJobService>(
            "monthly-performance-report",
            service => service.GenerateMonthlyPerformanceReportAsync(DateTime.UtcNow.AddMonths(-1), null),
            "0 4 1 * *", // 4 AM on the 1st of each month
            TimeZoneInfo.Utc,
            HangfireQueues.Reports);

        // Cleanup expired bookings
        RecurringJob.AddOrUpdate<CleanupJobService>(
            "cleanup-expired-bookings",
            service => service.CleanupExpiredBookingsAsync(null),
            "0 1 * * *", // 1 AM daily
            TimeZoneInfo.Utc,
            HangfireQueues.Cleanup);

        // System cleanup
        RecurringJob.AddOrUpdate<CleanupJobService>(
            "system-cleanup",
            service => service.SystemCleanupAsync(null),
            "0 0 * * 0", // Midnight every Sunday
            TimeZoneInfo.Utc,
            HangfireQueues.Cleanup);

        // Database maintenance
        RecurringJob.AddOrUpdate<CleanupJobService>(
            "database-maintenance",
            service => service.DatabaseMaintenanceAsync(null),
            "0 3 * * 0", // 3 AM every Sunday
            TimeZoneInfo.Utc,
            HangfireQueues.Cleanup);

        // Expire old promotions
        RecurringJob.AddOrUpdate<PricingJobService>(
            "expire-promotions",
            service => service.ExpireOldPromotionsAsync(null),
            "0 */6 * * *", // Every 6 hours
            TimeZoneInfo.Utc,
            HangfireQueues.Pricing);

        // Update seasonal pricing
        RecurringJob.AddOrUpdate<PricingJobService>(
            "seasonal-pricing-update",
            service => service.UpdateSeasonalPricingAsync(null),
            "0 5 * * *", // 5 AM daily
            TimeZoneInfo.Utc,
            HangfireQueues.Pricing);

        // Warm pricing cache
        RecurringJob.AddOrUpdate<PricingJobService>(
            "warm-pricing-cache",
            service => service.WarmPricingCacheAsync(null),
            "*/30 * * * *", // Every 30 minutes
            TimeZoneInfo.Utc,
            HangfireQueues.Pricing);

        // Process outbox events
        RecurringJob.AddOrUpdate<OutboxJobService>(
            "process-outbox-events",
            service => service.ProcessOutboxEventsAsync(null),
            "*/5 * * * *", // Every 5 minutes
            TimeZoneInfo.Utc,
            HangfireQueues.Critical);

        // Retry failed outbox events
        RecurringJob.AddOrUpdate<OutboxJobService>(
            "retry-failed-outbox-events",
            service => service.RetryFailedOutboxEventsAsync(null),
            "0 */2 * * *", // Every 2 hours
            TimeZoneInfo.Utc,
            HangfireQueues.Critical);

        // Cleanup processed outbox events
        RecurringJob.AddOrUpdate<OutboxJobService>(
            "cleanup-outbox-events",
            service => service.CleanupProcessedOutboxEventsAsync(null),
            "0 6 * * *", // 6 AM daily
            TimeZoneInfo.Utc,
            HangfireQueues.Cleanup);

        // ===== ANALYTICS JOBS =====

        // Refresh all analytics views daily
        RecurringJob.AddOrUpdate<AnalyticsJobService>(
            "refresh-all-analytics-views",
            service => service.RefreshAllAnalyticsViewsAsync(null),
            "0 2 * * *", // 2 AM daily
            TimeZoneInfo.Utc,
            HangfireQueues.Analytics);

        // Refresh revenue analytics hourly during business hours
        RecurringJob.AddOrUpdate<AnalyticsJobService>(
            "refresh-revenue-analytics",
            service => service.RefreshRevenueAnalyticsAsync(null),
            "0 8-18 * * *", // Every hour from 8 AM to 6 PM
            TimeZoneInfo.Utc,
            HangfireQueues.Analytics);

        // Refresh booking status analytics every 2 hours
        RecurringJob.AddOrUpdate<AnalyticsJobService>(
            "refresh-booking-status-analytics",
            service => service.RefreshBookingStatusAnalyticsAsync(null),
            "0 */2 * * *", // Every 2 hours
            TimeZoneInfo.Utc,
            HangfireQueues.Analytics);

        // Refresh passenger demographics daily
        RecurringJob.AddOrUpdate<AnalyticsJobService>(
            "refresh-passenger-demographics",
            service => service.RefreshPassengerDemographicsAsync(null),
            "0 3 * * *", // 3 AM daily
            TimeZoneInfo.Utc,
            HangfireQueues.Analytics);

        // Refresh route performance analytics daily
        RecurringJob.AddOrUpdate<AnalyticsJobService>(
            "refresh-route-performance-analytics",
            service => service.RefreshRoutePerformanceAnalyticsAsync(null),
            "0 4 * * *", // 4 AM daily
            TimeZoneInfo.Utc,
            HangfireQueues.Analytics);

        // Generate analytics health report daily
        RecurringJob.AddOrUpdate<AnalyticsJobService>(
            "analytics-health-report",
            service => service.GenerateAnalyticsHealthReportAsync(null),
            "0 6 * * *", // 6 AM daily
            TimeZoneInfo.Utc,
            HangfireQueues.Analytics);

        // Cleanup old analytics data weekly
        RecurringJob.AddOrUpdate<AnalyticsJobService>(
            "cleanup-old-analytics-data",
            service => service.CleanupOldAnalyticsDataAsync(null),
            "0 1 * * 0", // 1 AM every Sunday
            TimeZoneInfo.Utc,
            HangfireQueues.Cleanup);

        // Optimize analytics indexes weekly
        RecurringJob.AddOrUpdate<AnalyticsJobService>(
            "optimize-analytics-indexes",
            service => service.OptimizeAnalyticsIndexesAsync(null),
            "0 23 * * 6", // 11 PM every Saturday
            TimeZoneInfo.Utc,
            HangfireQueues.Cleanup);

        return app;
    }

    /// <summary>
    /// Add Hangfire worker host services
    /// </summary>
    public static IServiceCollection AddHangfireWorkerHost(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfireServices(configuration);
        services.AddHostedService<HangfireWorkerHostedService>();
        return services;
    }
}

/// <summary>
/// Custom authorization filter for Hangfire dashboard
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _requiredPolicy;

    public HangfireAuthorizationFilter(string requiredPolicy)
    {
        _requiredPolicy = requiredPolicy ?? throw new ArgumentNullException(nameof(requiredPolicy));
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated == true)
            return false;

        // Check if user has required policy/role
        if (!string.IsNullOrEmpty(_requiredPolicy))
        {
            var authorizationService = httpContext.RequestServices.GetService<IAuthorizationService>();
            if (authorizationService != null)
            {
                var result = authorizationService.AuthorizeAsync(httpContext.User, _requiredPolicy).Result;
                return result.Succeeded;
            }

            // Fallback to role check
            return httpContext.User.IsInRole(_requiredPolicy);
        }

        return true;
    }
}

/// <summary>
/// Hosted service for running Hangfire worker
/// </summary>
public class HangfireWorkerHostedService : IHostedService
{
    private readonly ILogger<HangfireWorkerHostedService> _logger;
    private readonly IOptions<HangfireConfiguration> _config;

    public HangfireWorkerHostedService(
        ILogger<HangfireWorkerHostedService> logger,
        IOptions<HangfireConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting Hangfire worker host. Server: {ServerName}, Workers: {WorkerCount}, Queues: {Queues}",
            _config.Value.Server.ServerName,
            _config.Value.Server.WorkerCount,
            string.Join(", ", _config.Value.Server.Queues));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Hangfire worker host");
        return Task.CompletedTask;
    }
}

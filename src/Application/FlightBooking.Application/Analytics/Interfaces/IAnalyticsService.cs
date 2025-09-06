using FlightBooking.Domain.Analytics;

namespace FlightBooking.Application.Analytics.Interfaces;

/// <summary>
/// Analytics service interface for revenue, booking, and demographic analytics
/// </summary>
public interface IAnalyticsService
{
    // Revenue Analytics
    Task<IEnumerable<RevenueAnalytics>> GetRevenueAnalyticsAsync(
        AnalyticsFilter filter, 
        CancellationToken cancellationToken = default);
    
    Task<RevenueBreakdown> GetRevenueBreakdownAsync(
        DateRange dateRange, 
        string? routeCode = null, 
        string? fareClass = null,
        CancellationToken cancellationToken = default);
    
    Task<decimal> GetTotalRevenueAsync(
        DateRange dateRange, 
        CancellationToken cancellationToken = default);
    
    // Booking Status Analytics
    Task<IEnumerable<BookingStatusAnalytics>> GetBookingStatusAnalyticsAsync(
        AnalyticsFilter filter, 
        CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, int>> GetBookingStatusSummaryAsync(
        DateRange dateRange, 
        CancellationToken cancellationToken = default);
    
    // Passenger Demographics
    Task<IEnumerable<PassengerDemographicsAnalytics>> GetPassengerDemographicsAsync(
        AnalyticsFilter filter, 
        CancellationToken cancellationToken = default);
    
    Task<DemographicsBreakdown> GetDemographicsBreakdownAsync(
        DateRange dateRange, 
        string? routeCode = null,
        CancellationToken cancellationToken = default);
    
    // Route Performance
    Task<IEnumerable<RoutePerformanceAnalytics>> GetRoutePerformanceAsync(
        AnalyticsFilter filter, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<RoutePerformanceAnalytics>> GetTopPerformingRoutesAsync(
        DateRange dateRange, 
        int topCount = 10,
        string orderBy = "TotalRevenue",
        CancellationToken cancellationToken = default);
    
    // Summary Analytics
    Task<AnalyticsSummary> GetAnalyticsSummaryAsync(
        DateRange dateRange, 
        CancellationToken cancellationToken = default);
    
    // Comparative Analytics
    Task<Dictionary<string, decimal>> GetRevenueComparisonAsync(
        DateRange currentPeriod, 
        DateRange previousPeriod,
        CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, decimal>> GetPerformanceComparisonAsync(
        DateRange currentPeriod, 
        DateRange previousPeriod,
        CancellationToken cancellationToken = default);
    
    // Trend Analytics
    Task<IEnumerable<(DateTime Date, decimal Revenue)>> GetRevenueTrendAsync(
        DateRange dateRange, 
        AnalyticsPeriod period = AnalyticsPeriod.Daily,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<(DateTime Date, int Bookings)>> GetBookingTrendAsync(
        DateRange dateRange, 
        AnalyticsPeriod period = AnalyticsPeriod.Daily,
        CancellationToken cancellationToken = default);
    
    // Data Quality
    Task<DateTime> GetLastRefreshTimeAsync(string viewName, CancellationToken cancellationToken = default);
    Task<bool> IsDataCurrentAsync(string viewName, TimeSpan maxAge, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetDataQualityMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// CSV export service interface
/// </summary>
public interface ICsvExportService
{
    Task<byte[]> ExportRevenueAnalyticsAsync(
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportBookingStatusAnalyticsAsync(
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportPassengerDemographicsAsync(
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportRoutePerformanceAsync(
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportAnalyticsSummaryAsync(
        DateRange dateRange, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default);
    
    Task<string> GetExportUrlAsync(
        string exportType, 
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Analytics cache service interface
/// </summary>
public interface IAnalyticsCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    string GenerateKey(string prefix, params object[] parameters);
}

/// <summary>
/// Analytics materialized view refresh service interface
/// </summary>
public interface IAnalyticsRefreshService
{
    Task RefreshAllViewsAsync(CancellationToken cancellationToken = default);
    Task RefreshViewAsync(string viewName, CancellationToken cancellationToken = default);
    Task<bool> IsRefreshInProgressAsync(string viewName, CancellationToken cancellationToken = default);
    Task<DateTime> GetLastRefreshTimeAsync(string viewName, CancellationToken cancellationToken = default);
    Task<TimeSpan> GetRefreshDurationAsync(string viewName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetRefreshStatusAsync(CancellationToken cancellationToken = default);
}

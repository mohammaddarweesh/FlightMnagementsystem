# Flight Booking Analytics System

## Overview

The Flight Booking Analytics System provides comprehensive business intelligence and reporting capabilities for the flight booking platform. It uses PostgreSQL materialized views for high-performance analytics queries and includes automated data refresh, caching, and export functionality.

## Architecture

### Core Components

1. **Domain Models** (`FlightBooking.Domain.Analytics`)
   - `RevenueAnalytics` - Revenue metrics and KPIs
   - `BookingStatusAnalytics` - Booking lifecycle and conversion metrics
   - `PassengerDemographicsAnalytics` - Customer demographic insights
   - `RoutePerformanceAnalytics` - Route-specific performance metrics
   - `AnalyticsSummary` - High-level business overview

2. **Application Services** (`FlightBooking.Application.Analytics`)
   - `IAnalyticsService` - Core analytics data retrieval
   - `ICsvExportService` - Data export functionality
   - `IAnalyticsRefreshService` - Materialized view management
   - `IAnalyticsCacheService` - Performance caching

3. **Infrastructure Implementation** (`FlightBooking.Infrastructure.Analytics`)
   - `AnalyticsService` - Main analytics implementation
   - `CsvExportService` - CSV export implementation
   - `AnalyticsRefreshService` - View refresh management
   - `AnalyticsCacheService` - Redis/In-memory caching

4. **API Controllers** (`FlightBooking.Api.Controllers`)
   - `AnalyticsController` - Analytics data endpoints
   - `AnalyticsExportController` - Export endpoints

5. **Background Jobs** (`FlightBooking.Infrastructure.BackgroundJobs`)
   - `AnalyticsJobService` - Automated refresh and maintenance

## Database Schema

### Materialized Views

The system uses PostgreSQL materialized views for optimal query performance:

- **`mv_revenue_daily`** - Daily revenue aggregations
- **`mv_booking_status_daily`** - Daily booking status metrics
- **`mv_passenger_demographics_daily`** - Daily demographic breakdowns
- **`mv_route_performance_daily`** - Daily route performance metrics

### Refresh Log Table

- **`analytics_refresh_log`** - Tracks materialized view refresh history

## API Endpoints

### Analytics Data Endpoints

```
GET /api/analytics/summary
GET /api/analytics/revenue
GET /api/analytics/revenue/breakdown
GET /api/analytics/bookings
GET /api/analytics/bookings/summary
GET /api/analytics/demographics
GET /api/analytics/demographics/breakdown
GET /api/analytics/routes
GET /api/analytics/routes/top
GET /api/analytics/trends/revenue
GET /api/analytics/trends/bookings
GET /api/analytics/compare/revenue
GET /api/analytics/data-quality
```

### Export Endpoints

```
GET /api/analytics/export/revenue/csv
GET /api/analytics/export/bookings/csv
GET /api/analytics/export/demographics/csv
GET /api/analytics/export/routes/csv
GET /api/analytics/export/summary/csv
POST /api/analytics/export/url
POST /api/analytics/export/bulk
```

## Background Jobs

### Scheduled Analytics Jobs

| Job | Schedule | Description |
|-----|----------|-------------|
| `refresh-all-analytics-views` | Daily 2 AM | Full refresh of all materialized views |
| `refresh-revenue-analytics` | Hourly 8 AM-6 PM | Revenue view refresh during business hours |
| `refresh-booking-status-analytics` | Every 2 hours | Booking status metrics refresh |
| `refresh-passenger-demographics` | Daily 3 AM | Demographics view refresh |
| `refresh-route-performance-analytics` | Daily 4 AM | Route performance refresh |
| `analytics-health-report` | Daily 6 AM | Generate health status report |
| `cleanup-old-analytics-data` | Weekly Sunday 1 AM | Remove old analytics logs |
| `optimize-analytics-indexes` | Weekly Saturday 11 PM | Reindex materialized views |

## Configuration

### Analytics Settings

```json
{
  "Analytics": {
    "CacheProvider": "Redis", // or "Memory"
    "CacheExpirationMinutes": 60,
    "MaxExportRows": 100000,
    "DefaultDateRange": 30,
    "RefreshTimeoutMinutes": 30
  }
}
```

### Authorization Policies

- **`AdminOrStaff`** - Read access to analytics data
- **`AnalyticsExport`** - CSV export permissions
- **`AnalyticsAdmin`** - Administrative operations

## Usage Examples

### Get Revenue Analytics

```csharp
var filter = new AnalyticsFilter
{
    DateRange = new DateRange(DateTime.Today.AddDays(-30), DateTime.Today),
    Period = AnalyticsPeriod.Daily,
    RouteCodes = new[] { "NYC-LAX" }
};

var revenue = await analyticsService.GetRevenueAnalyticsAsync(filter);
```

### Export to CSV

```csharp
var config = new ExportConfiguration
{
    Format = ExportFormat.CSV,
    IncludeHeaders = true,
    IncludeMetadata = true,
    MaxRows = 10000
};

var csvData = await csvExportService.ExportRevenueAnalyticsAsync(filter, config);
```

### Manual View Refresh

```csharp
await analyticsRefreshService.RefreshViewAsync("revenue");
```

## Performance Considerations

### Caching Strategy

- **L1 Cache**: In-memory cache for frequently accessed data (5-15 minutes)
- **L2 Cache**: Redis distributed cache for shared data (30-60 minutes)
- **L3 Cache**: Materialized views for complex aggregations (refreshed hourly/daily)

### Query Optimization

- Materialized views with optimized indexes
- Partitioning by date for large datasets
- Concurrent refresh to minimize downtime
- Query result pagination for large exports

### Monitoring

- Health checks for materialized view freshness
- Data quality metrics and alerts
- Performance monitoring for refresh operations
- Export operation tracking

## Data Quality

### Quality Metrics

- **Completeness**: Percentage of expected records present
- **Accuracy**: Data validation against source systems
- **Timeliness**: Freshness of materialized views
- **Consistency**: Cross-view data consistency checks

### Quality Grades

- **A (90-100%)**: Excellent data quality
- **B (80-89%)**: Good data quality
- **C (70-79%)**: Acceptable data quality
- **D (60-69%)**: Poor data quality
- **F (<60%)**: Unacceptable data quality

## Troubleshooting

### Common Issues

1. **Stale Data**
   - Check materialized view refresh status
   - Verify background job execution
   - Review refresh error logs

2. **Performance Issues**
   - Monitor cache hit rates
   - Check index usage
   - Review query execution plans

3. **Export Failures**
   - Verify row limits and timeouts
   - Check available disk space
   - Review export configuration

### Health Checks

Access health status at:
- `/health` - Overall system health
- `/health/analytics_materialized_views` - View health
- `/health/analytics_data_quality` - Data quality status

## Security

### Access Control

- Role-based access control (RBAC)
- API key authentication for exports
- Rate limiting on export endpoints
- Audit logging for all analytics access

### Data Privacy

- PII anonymization in demographics
- Aggregated data only (no individual records)
- Secure export file handling
- Automatic cleanup of temporary files

## Maintenance

### Regular Tasks

1. **Weekly**: Review data quality reports
2. **Monthly**: Analyze performance metrics
3. **Quarterly**: Review and optimize queries
4. **Annually**: Archive old analytics data

### Backup and Recovery

- Materialized views can be rebuilt from source data
- Export configurations stored in version control
- Refresh logs backed up with main database
- Recovery procedures documented in runbooks

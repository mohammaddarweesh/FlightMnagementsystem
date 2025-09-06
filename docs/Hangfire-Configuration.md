# Hangfire Configuration Documentation

## Overview

This document describes the comprehensive Hangfire configuration implemented in the Flight Booking System, featuring PostgreSQL storage, multiple queues, exponential backoff retry policies, dead letter queue support, and robust observability.

## Architecture

### Components

1. **API Server** - Lightweight Hangfire server for critical jobs only
2. **Dedicated Worker Host** - Heavy-duty background job processing
3. **PostgreSQL Storage** - Reliable job persistence with schema isolation
4. **Dead Letter Queue** - Custom implementation for failed job tracking
5. **Dashboard** - Secure web interface for monitoring and management

## Queue Configuration

### Queue Hierarchy (Priority Order)

1. **Critical Queue** (`critical`)
   - **Purpose**: Outbox processing, payment confirmations
   - **Workers**: 3 (API) + 3 (Worker) = 6 total
   - **Timeout**: 10 minutes
   - **Retry**: 5 attempts with exponential backoff

2. **Emails Queue** (`emails`)
   - **Purpose**: All email communications
   - **Workers**: 2 (API) + 4 (Worker) = 6 total
   - **Timeout**: 5 minutes
   - **Retry**: 3 attempts with 60s base delay

3. **Reports Queue** (`reports`)
   - **Purpose**: Revenue reports, analytics
   - **Workers**: 0 (API) + 2 (Worker) = 2 total
   - **Timeout**: 2 hours
   - **Retry**: 2 attempts with 5-minute base delay

4. **Cleanup Queue** (`cleanup`)
   - **Purpose**: Maintenance, archival, database cleanup
   - **Workers**: 0 (API) + 1 (Worker) = 1 total
   - **Timeout**: 1 hour
   - **Retry**: 3 attempts with 10-minute base delay

5. **Pricing Queue** (`pricing`)
   - **Purpose**: Dynamic pricing, cache warming
   - **Workers**: 0 (API) + 2 (Worker) = 2 total
   - **Timeout**: 15 minutes
   - **Retry**: 4 attempts with 2-minute base delay

6. **Default Queue** (`default`)
   - **Purpose**: General background tasks
   - **Workers**: 2 (API) + 2 (Worker) = 4 total
   - **Timeout**: 30 minutes
   - **Retry**: 5 attempts with 30s base delay

## Job Types and Scheduling

### Email Jobs

```csharp
// Booking confirmation (immediate)
BackgroundJob.Enqueue<EmailJobService>(x => 
    x.SendBookingConfirmationEmailAsync(bookingId, email, name, reference, correlationId));

// Booking reminder (scheduled)
BackgroundJob.Schedule<EmailJobService>(
    x => x.SendBookingReminderEmailAsync(bookingId, email, name, reference, departureTime, flightNumber, correlationId),
    departureTime.AddHours(-24));

// Cancellation notification (immediate)
BackgroundJob.Enqueue<EmailJobService>(x => 
    x.SendBookingCancellationEmailAsync(bookingId, email, name, reference, reason, refund, correlationId));
```

### Report Jobs (Recurring)

```csharp
// Nightly revenue report - 2 AM daily
RecurringJob.AddOrUpdate<ReportJobService>(
    "nightly-revenue-report",
    service => service.GenerateNightlyRevenueReportAsync(DateTime.UtcNow.Date, null),
    "0 2 * * *");

// Weekly summary - 3 AM every Monday
RecurringJob.AddOrUpdate<ReportJobService>(
    "weekly-booking-summary",
    service => service.GenerateWeeklyBookingSummaryAsync(DateTime.UtcNow.AddDays(-7).Date, null),
    "0 3 * * 1");

// Monthly performance - 4 AM on 1st of month
RecurringJob.AddOrUpdate<ReportJobService>(
    "monthly-performance-report",
    service => service.GenerateMonthlyPerformanceReportAsync(DateTime.UtcNow.AddMonths(-1), null),
    "0 4 1 * *");
```

### Cleanup Jobs (Recurring)

```csharp
// Daily cleanup - 1 AM
RecurringJob.AddOrUpdate<CleanupJobService>(
    "cleanup-expired-bookings",
    service => service.CleanupExpiredBookingsAsync(null),
    "0 1 * * *");

// Weekly system cleanup - Midnight Sunday
RecurringJob.AddOrUpdate<CleanupJobService>(
    "system-cleanup",
    service => service.SystemCleanupAsync(null),
    "0 0 * * 0");

// Database maintenance - 3 AM Sunday
RecurringJob.AddOrUpdate<CleanupJobService>(
    "database-maintenance",
    service => service.DatabaseMaintenanceAsync(null),
    "0 3 * * 0");
```

### Pricing Jobs

```csharp
// Dynamic pricing update (on-demand)
BackgroundJob.Enqueue<PricingJobService>(x => 
    x.UpdateFlightPricingAsync(flightId, correlationId));

// Expire promotions - Every 6 hours
RecurringJob.AddOrUpdate<PricingJobService>(
    "expire-promotions",
    service => service.ExpireOldPromotionsAsync(null),
    "0 */6 * * *");

// Cache warming - Every 30 minutes
RecurringJob.AddOrUpdate<PricingJobService>(
    "warm-pricing-cache",
    service => service.WarmPricingCacheAsync(null),
    "*/30 * * * *");
```

### Outbox Processing

```csharp
// Process outbox events - Every 5 minutes
RecurringJob.AddOrUpdate<OutboxJobService>(
    "process-outbox-events",
    service => service.ProcessOutboxEventsAsync(null),
    "*/5 * * * *");

// Retry failed events - Every 2 hours
RecurringJob.AddOrUpdate<OutboxJobService>(
    "retry-failed-outbox-events",
    service => service.RetryFailedOutboxEventsAsync(null),
    "0 */2 * * *");
```

## Retry Policies

### Exponential Backoff Implementation

```csharp
[ExponentialBackoffRetryAttribute(
    maxAttempts: 5,
    baseDelaySeconds: 30,
    maxDelaySeconds: 3600,
    backoffMultiplier: 2.0,
    enableJitter: true,
    jitterFactor: 0.1)]
```

### Specialized Retry Attributes

- **EmailRetryAttribute**: 3 attempts, 60s base, 30min max
- **ReportRetryAttribute**: 2 attempts, 5min base, 1hr max
- **CleanupRetryAttribute**: 3 attempts, 10min base, 2hr max
- **PricingRetryAttribute**: 4 attempts, 2min base, 30min max
- **CriticalRetryAttribute**: 5 attempts, 30s base, 10min max

### Retry Delay Calculation

```
delay = baseDelay * (backoffMultiplier ^ (attempt - 1))
if (enableJitter) {
    jitter = 1.0 + (random(-0.5, 0.5) * 2 * jitterFactor)
    delay *= jitter
}
delay = min(delay, maxDelay)
```

## Dead Letter Queue

### Features

- **Automatic Capture**: Failed jobs after max retries
- **Rich Metadata**: Job details, exception info, retry history
- **Correlation Tracking**: End-to-end traceability
- **Requeue Support**: Manual and bulk requeue operations
- **Analytics**: Failure trends and statistics
- **Cleanup**: Automatic retention management

### Dead Letter Queue Schema

```sql
CREATE TABLE hangfire.job_dead_letter_queue (
    id UUID PRIMARY KEY,
    job_id VARCHAR(100) NOT NULL,
    correlation_id VARCHAR(100),
    job_type VARCHAR(500) NOT NULL,
    method_name VARCHAR(200) NOT NULL,
    arguments TEXT,
    queue_name VARCHAR(100) NOT NULL,
    retry_attempts INTEGER NOT NULL,
    exception_message TEXT,
    exception_details TEXT,
    created_at TIMESTAMP NOT NULL,
    first_failed_at TIMESTAMP NOT NULL,
    moved_to_dead_letter_at TIMESTAMP NOT NULL,
    server_name VARCHAR(200),
    metadata TEXT,
    is_requeued BOOLEAN DEFAULT FALSE,
    requeued_at TIMESTAMP,
    requeued_by VARCHAR(200)
);
```

### Dead Letter Queue Operations

```csharp
// Get failed jobs
var query = new DeadLetterQueueQuery
{
    QueueName = "emails",
    FromDate = DateTime.UtcNow.AddDays(-7)
};
var result = await _deadLetterService.GetEntriesAsync(query);

// Requeue specific job
await _deadLetterService.RequeueEntryAsync(entryId, "admin@company.com");

// Bulk requeue by criteria
await _deadLetterService.BulkRequeueAsync(
    queueName: "emails",
    fromDate: DateTime.UtcNow.AddDays(-1),
    requeuedBy: "system");

// Get statistics
var stats = await _deadLetterService.GetStatsAsync();
```

## Observability

### Correlation ID Tracking

Every job includes a correlation ID for end-to-end tracing:

```csharp
public async Task SendBookingConfirmationEmailAsync(
    Guid bookingId,
    string customerEmail,
    string customerName,
    string bookingReference,
    string? correlationId = null)
{
    var jobId = BackgroundJob.CurrentJobId;
    correlationId ??= Guid.NewGuid().ToString();
    
    _logger.LogInformation(
        "Starting booking confirmation email job. JobId: {JobId}, BookingId: {BookingId}, " +
        "CustomerEmail: {CustomerEmail}, CorrelationId: {CorrelationId}",
        jobId, bookingId, customerEmail, correlationId);
}
```

### Structured Logging

All jobs use structured logging with consistent fields:
- **JobId**: Hangfire job identifier
- **CorrelationId**: End-to-end trace identifier
- **JobType**: Class and method name
- **Duration**: Job execution time
- **RetryAttempt**: Current retry number
- **QueueName**: Processing queue

### Metrics and Monitoring

- **Job Success/Failure Rates**: Per queue and job type
- **Processing Times**: Average, P95, P99 percentiles
- **Queue Depths**: Current and historical
- **Retry Patterns**: Failure analysis
- **Dead Letter Trends**: Failure hotspots

## Dashboard Security

### Authentication Requirements

```json
{
  "Hangfire": {
    "Dashboard": {
      "Enabled": true,
      "RequireAuthentication": true,
      "RequiredPolicy": "Admin"
    }
  }
}
```

### Authorization Filter

```csharp
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        if (!httpContext.User.Identity?.IsAuthenticated == true)
            return false;

        var authorizationService = httpContext.RequestServices.GetService<IAuthorizationService>();
        var result = authorizationService.AuthorizeAsync(httpContext.User, "Admin").Result;
        return result.Succeeded;
    }
}
```

## Deployment

### API Server Configuration

```json
{
  "Hangfire": {
    "Server": {
      "ServerName": "FlightBooking-API",
      "WorkerCount": 5,
      "Queues": ["critical", "emails", "default"]
    }
  }
}
```

### Worker Host Configuration

```json
{
  "Hangfire": {
    "Server": {
      "ServerName": "FlightBooking-Worker-01",
      "WorkerCount": 10,
      "Queues": ["critical", "emails", "reports", "cleanup", "pricing", "default"]
    }
  }
}
```

### Running the Worker Host

```bash
# Development
dotnet run --project src/Workers/FlightBooking.Workers

# Production (as Windows Service)
sc create "FlightBooking.Workers" binPath="C:\Apps\FlightBooking.Workers\FlightBooking.Workers.exe"
sc start "FlightBooking.Workers"

# Production (as Linux systemd service)
sudo systemctl enable flightbooking-workers
sudo systemctl start flightbooking-workers
```

## Database Setup

### PostgreSQL Schema Creation

```sql
-- Create Hangfire database
CREATE DATABASE FlightBookingHangfire;

-- Create schema
CREATE SCHEMA hangfire;

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE FlightBookingHangfire TO flightbooking_user;
GRANT ALL PRIVILEGES ON SCHEMA hangfire TO flightbooking_user;
```

### Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=FlightBookingDb;Username=postgres;Password=password",
    "Hangfire": "Host=localhost;Database=FlightBookingHangfire;Username=postgres;Password=password"
  }
}
```

## Performance Tuning

### Worker Scaling

- **API Server**: Minimal workers for critical jobs only
- **Worker Host**: Dedicated processing with queue-specific workers
- **Queue Priorities**: Critical > Emails > Reports > Cleanup > Pricing > Default

### Database Optimization

- **Connection Pooling**: Configured in PostgreSQL storage
- **Index Optimization**: Hangfire creates necessary indexes
- **Maintenance**: Weekly VACUUM ANALYZE for performance

### Memory Management

- **Job Serialization**: Minimal payload sizes
- **Batch Processing**: Chunked operations for large datasets
- **Cleanup**: Automatic removal of old job data

## Monitoring and Alerting

### Key Metrics to Monitor

1. **Queue Depths**: Alert if queues grow beyond thresholds
2. **Job Failure Rates**: Alert on high failure percentages
3. **Processing Times**: Alert on performance degradation
4. **Dead Letter Growth**: Alert on increasing failure patterns
5. **Worker Health**: Monitor worker availability

### Sample Alerts

```yaml
# Queue depth alert
- alert: HangfireQueueDepth
  expr: hangfire_queue_depth{queue="critical"} > 100
  for: 5m
  labels:
    severity: warning

# High failure rate alert
- alert: HangfireHighFailureRate
  expr: rate(hangfire_jobs_failed_total[5m]) > 0.1
  for: 2m
  labels:
    severity: critical
```

## Troubleshooting

### Common Issues

1. **Jobs Stuck in Processing**: Check worker health and database connectivity
2. **High Retry Rates**: Review job logic and external dependencies
3. **Dead Letter Growth**: Analyze failure patterns and fix root causes
4. **Performance Issues**: Check database performance and worker scaling

### Diagnostic Commands

```csharp
// Check queue status
var monitoring = JobStorage.Current.GetMonitoringApi();
var queues = monitoring.Queues();

// Get job details
var jobDetails = monitoring.JobDetails(jobId);

// Check server status
var servers = monitoring.Servers();
```

This Hangfire configuration provides enterprise-grade background job processing with robust reliability, observability, and operational features essential for a production flight booking system.

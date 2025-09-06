using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Audit;

public class AuditOutbox : BaseEntity
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? GuestId { get; set; }
    public string Route { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public int StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? ResultSummary { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserEmail { get; set; }
    public string? UserRoles { get; set; }
    public long? RequestSize { get; set; }
    public long? ResponseSize { get; set; }
    public string? Headers { get; set; }
    public string? QueryParameters { get; set; }
    
    // Outbox specific properties
    public bool IsProcessed { get; set; } = false;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ProcessingError { get; set; }
    public DateTime? NextRetryAt { get; set; }

    // Constants
    public const int MaxRetryCount = 3;
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    // Helper methods
    public static AuditOutbox FromAuditEvent(AuditEvent auditEvent)
    {
        return new AuditOutbox
        {
            CorrelationId = auditEvent.CorrelationId,
            UserId = auditEvent.UserId,
            GuestId = auditEvent.GuestId,
            Route = auditEvent.Route,
            HttpMethod = auditEvent.HttpMethod,
            IpAddress = auditEvent.IpAddress,
            UserAgent = auditEvent.UserAgent,
            StatusCode = auditEvent.StatusCode,
            LatencyMs = auditEvent.LatencyMs,
            RequestBody = auditEvent.RequestBody,
            ResponseBody = auditEvent.ResponseBody,
            ResultSummary = auditEvent.ResultSummary,
            ErrorMessage = auditEvent.ErrorMessage,
            Timestamp = auditEvent.Timestamp,
            UserEmail = auditEvent.UserEmail,
            UserRoles = auditEvent.UserRoles,
            RequestSize = auditEvent.RequestSize,
            ResponseSize = auditEvent.ResponseSize,
            Headers = auditEvent.Headers,
            QueryParameters = auditEvent.QueryParameters
        };
    }

    public AuditEvent ToAuditEvent()
    {
        return new AuditEvent
        {
            CorrelationId = CorrelationId,
            UserId = UserId,
            GuestId = GuestId,
            Route = Route,
            HttpMethod = HttpMethod,
            IpAddress = IpAddress,
            UserAgent = UserAgent,
            StatusCode = StatusCode,
            LatencyMs = LatencyMs,
            RequestBody = RequestBody,
            ResponseBody = ResponseBody,
            ResultSummary = ResultSummary,
            ErrorMessage = ErrorMessage,
            Timestamp = Timestamp,
            UserEmail = UserEmail,
            UserRoles = UserRoles,
            RequestSize = RequestSize,
            ResponseSize = ResponseSize,
            Headers = Headers,
            QueryParameters = QueryParameters
        };
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
        ProcessingError = null;
        NextRetryAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailedWithRetry(string error)
    {
        RetryCount++;
        ProcessingError = error;
        
        if (RetryCount >= MaxRetryCount)
        {
            // Mark as permanently failed
            NextRetryAt = null;
        }
        else
        {
            // Schedule next retry with exponential backoff
            var delay = TimeSpan.FromMinutes(RetryDelay.TotalMinutes * Math.Pow(2, RetryCount - 1));
            NextRetryAt = DateTime.UtcNow.Add(delay);
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public bool ShouldRetry()
    {
        return !IsProcessed && 
               RetryCount < MaxRetryCount && 
               (NextRetryAt == null || NextRetryAt <= DateTime.UtcNow);
    }
}

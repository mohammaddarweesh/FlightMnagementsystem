using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Audit;

public class AuditEvent : BaseEntity
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

    // Computed properties
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    public bool IsClientError => StatusCode >= 400 && StatusCode < 500;
    public bool IsServerError => StatusCode >= 500;
    public bool IsSlowRequest => LatencyMs > 1000; // > 1 second
    public string UserIdentifier => UserId?.ToString() ?? GuestId ?? "Anonymous";

    // Helper methods
    public static AuditEvent Create(
        string correlationId,
        string route,
        string httpMethod,
        string ipAddress,
        Guid? userId = null,
        string? guestId = null)
    {
        return new AuditEvent
        {
            CorrelationId = correlationId,
            Route = route,
            HttpMethod = httpMethod,
            IpAddress = ipAddress,
            UserId = userId,
            GuestId = guestId,
            Timestamp = DateTime.UtcNow
        };
    }

    public void SetResult(int statusCode, long latencyMs, string? resultSummary = null)
    {
        StatusCode = statusCode;
        LatencyMs = latencyMs;
        ResultSummary = resultSummary;
    }

    public void SetError(string errorMessage)
    {
        ErrorMessage = errorMessage;
        ResultSummary = $"Error: {errorMessage}";
    }

    public void SetUserContext(string? email, string? roles)
    {
        UserEmail = email;
        UserRoles = roles;
    }

    public void SetRequestDetails(string? body, long? size, string? headers, string? queryParams)
    {
        RequestBody = body;
        RequestSize = size;
        Headers = headers;
        QueryParameters = queryParams;
    }

    public void SetResponseDetails(string? body, long? size)
    {
        ResponseBody = body;
        ResponseSize = size;
    }
}

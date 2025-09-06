using FlightBooking.Contracts.Common;

namespace FlightBooking.Contracts.Audit;

public class AuditEventDto
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? GuestId { get; set; }
    public string Route { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public int StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public string? ResultSummary { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRoles { get; set; }
    public long? RequestSize { get; set; }
    public long? ResponseSize { get; set; }
    public string? QueryParameters { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsSlowRequest { get; set; }
    public string UserIdentifier { get; set; } = string.Empty;
}

public class AuditQueryResponse : BaseResponse
{
    public List<AuditEventDto> Events { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class AuditStatsDto
{
    public string Period { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public long AverageLatencyMs { get; set; }
    public long MaxLatencyMs { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueGuests { get; set; }
    public Dictionary<string, int> StatusCodeCounts { get; set; } = new();
    public Dictionary<string, int> TopRoutes { get; set; } = new();
    public Dictionary<string, int> TopUserAgents { get; set; } = new();
}

public class AuditStatsResponse : BaseResponse
{
    public List<AuditStatsDto> Stats { get; set; } = new();
    public AuditStatsDto Summary { get; set; } = new();
}

public class AuditDetailDto : AuditEventDto
{
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? Headers { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AuditDetailResponse : BaseResponse
{
    public AuditDetailDto? Event { get; set; }
}

public class AuditExportResponse : BaseResponse
{
    public string? DownloadUrl { get; set; }
    public string? FileName { get; set; }
    public int RecordCount { get; set; }
    public string Format { get; set; } = string.Empty;
}

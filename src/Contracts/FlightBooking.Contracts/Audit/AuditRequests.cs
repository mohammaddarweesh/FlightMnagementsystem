namespace FlightBooking.Contracts.Audit;

public class AuditQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? UserId { get; set; }
    public string? GuestId { get; set; }
    public string? Route { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public string? UserEmail { get; set; }
    public long? MinLatencyMs { get; set; }
    public long? MaxLatencyMs { get; set; }
    public bool? HasErrors { get; set; }
    public string? CorrelationId { get; set; }
    public string? SortBy { get; set; } = "Timestamp";
    public string? SortDirection { get; set; } = "desc";
}

public class AuditStatsRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? GroupBy { get; set; } = "hour"; // hour, day, week, month
}

public class AuditExportRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? UserId { get; set; }
    public string? Route { get; set; }
    public string? Format { get; set; } = "csv"; // csv, json
    public int MaxRecords { get; set; } = 10000;
}

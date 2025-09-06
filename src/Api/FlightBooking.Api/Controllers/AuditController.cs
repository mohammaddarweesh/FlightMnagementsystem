using FlightBooking.Contracts.Audit;
using FlightBooking.Contracts.Common;
using FlightBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "StaffPolicy")]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditController> _logger;

    public AuditController(ApplicationDbContext context, ILogger<AuditController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Query audit events with filtering and pagination
    /// </summary>
    [HttpGet("events")]
    public async Task<ActionResult<AuditQueryResponse>> GetAuditEvents([FromQuery] AuditQueryRequest request)
    {
        try
        {
            var query = _context.AuditEvents.AsQueryable();

            // Apply filters
            if (request.StartDate.HasValue)
                query = query.Where(ae => ae.Timestamp >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(ae => ae.Timestamp <= request.EndDate.Value);

            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var userId))
                query = query.Where(ae => ae.UserId == userId);

            if (!string.IsNullOrEmpty(request.GuestId))
                query = query.Where(ae => ae.GuestId == request.GuestId);

            if (!string.IsNullOrEmpty(request.Route))
                query = query.Where(ae => ae.Route.Contains(request.Route));

            if (!string.IsNullOrEmpty(request.HttpMethod))
                query = query.Where(ae => ae.HttpMethod == request.HttpMethod);

            if (request.StatusCode.HasValue)
                query = query.Where(ae => ae.StatusCode == request.StatusCode.Value);

            if (!string.IsNullOrEmpty(request.IpAddress))
                query = query.Where(ae => ae.IpAddress == request.IpAddress);

            if (!string.IsNullOrEmpty(request.UserEmail))
                query = query.Where(ae => ae.UserEmail != null && ae.UserEmail.Contains(request.UserEmail));

            if (request.MinLatencyMs.HasValue)
                query = query.Where(ae => ae.LatencyMs >= request.MinLatencyMs.Value);

            if (request.MaxLatencyMs.HasValue)
                query = query.Where(ae => ae.LatencyMs <= request.MaxLatencyMs.Value);

            if (request.HasErrors.HasValue)
            {
                if (request.HasErrors.Value)
                    query = query.Where(ae => ae.ErrorMessage != null);
                else
                    query = query.Where(ae => ae.ErrorMessage == null);
            }

            if (!string.IsNullOrEmpty(request.CorrelationId))
                query = query.Where(ae => ae.CorrelationId == request.CorrelationId);

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "timestamp" => request.SortDirection?.ToLower() == "asc" 
                    ? query.OrderBy(ae => ae.Timestamp)
                    : query.OrderByDescending(ae => ae.Timestamp),
                "latency" => request.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(ae => ae.LatencyMs)
                    : query.OrderByDescending(ae => ae.LatencyMs),
                "statuscode" => request.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(ae => ae.StatusCode)
                    : query.OrderByDescending(ae => ae.StatusCode),
                "route" => request.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(ae => ae.Route)
                    : query.OrderByDescending(ae => ae.Route),
                _ => query.OrderByDescending(ae => ae.Timestamp)
            };

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var pageSize = Math.Min(request.PageSize, 100); // Max 100 per page
            var skip = (request.Page - 1) * pageSize;
            
            var events = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(ae => new AuditEventDto
                {
                    Id = ae.Id,
                    CorrelationId = ae.CorrelationId,
                    UserId = ae.UserId,
                    GuestId = ae.GuestId,
                    Route = ae.Route,
                    HttpMethod = ae.HttpMethod,
                    IpAddress = ae.IpAddress,
                    UserAgent = ae.UserAgent,
                    StatusCode = ae.StatusCode,
                    LatencyMs = ae.LatencyMs,
                    ResultSummary = ae.ResultSummary,
                    ErrorMessage = ae.ErrorMessage,
                    Timestamp = ae.Timestamp,
                    UserEmail = ae.UserEmail,
                    UserRoles = ae.UserRoles,
                    RequestSize = ae.RequestSize,
                    ResponseSize = ae.ResponseSize,
                    QueryParameters = ae.QueryParameters,
                    IsSuccess = ae.StatusCode >= 200 && ae.StatusCode < 300,
                    IsSlowRequest = ae.LatencyMs > 1000,
                    UserIdentifier = ae.UserId != null ? ae.UserId.ToString()! : ae.GuestId ?? "Anonymous"
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new AuditQueryResponse
            {
                Success = true,
                Events = events,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying audit events");
            return StatusCode(500, new AuditQueryResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while querying audit events"
            });
        }
    }

    /// <summary>
    /// Get detailed audit event by ID
    /// </summary>
    [HttpGet("events/{id}")]
    public async Task<ActionResult<AuditDetailResponse>> GetAuditEventDetail(Guid id)
    {
        try
        {
            var auditEvent = await _context.AuditEvents
                .Where(ae => ae.Id == id)
                .Select(ae => new AuditDetailDto
                {
                    Id = ae.Id,
                    CorrelationId = ae.CorrelationId,
                    UserId = ae.UserId,
                    GuestId = ae.GuestId,
                    Route = ae.Route,
                    HttpMethod = ae.HttpMethod,
                    IpAddress = ae.IpAddress,
                    UserAgent = ae.UserAgent,
                    StatusCode = ae.StatusCode,
                    LatencyMs = ae.LatencyMs,
                    RequestBody = ae.RequestBody,
                    ResponseBody = ae.ResponseBody,
                    ResultSummary = ae.ResultSummary,
                    ErrorMessage = ae.ErrorMessage,
                    Timestamp = ae.Timestamp,
                    UserEmail = ae.UserEmail,
                    UserRoles = ae.UserRoles,
                    RequestSize = ae.RequestSize,
                    ResponseSize = ae.ResponseSize,
                    Headers = ae.Headers,
                    QueryParameters = ae.QueryParameters,
                    CreatedAt = ae.CreatedAt,
                    UpdatedAt = ae.UpdatedAt,
                    IsSuccess = ae.StatusCode >= 200 && ae.StatusCode < 300,
                    IsSlowRequest = ae.LatencyMs > 1000,
                    UserIdentifier = ae.UserId != null ? ae.UserId.ToString()! : ae.GuestId ?? "Anonymous"
                })
                .FirstOrDefaultAsync();

            if (auditEvent == null)
            {
                return NotFound(new AuditDetailResponse
                {
                    Success = false,
                    ErrorMessage = "Audit event not found"
                });
            }

            return Ok(new AuditDetailResponse
            {
                Success = true,
                Event = auditEvent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit event detail for ID: {Id}", id);
            return StatusCode(500, new AuditDetailResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving audit event details"
            });
        }
    }

    /// <summary>
    /// Get audit statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<AuditStatsResponse>> GetAuditStats([FromQuery] AuditStatsRequest request)
    {
        try
        {
            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-7);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            var query = _context.AuditEvents
                .Where(ae => ae.Timestamp >= startDate && ae.Timestamp <= endDate);

            // Overall summary
            var summary = await query
                .GroupBy(ae => 1)
                .Select(g => new AuditStatsDto
                {
                    Period = "Summary",
                    TotalRequests = g.Count(),
                    SuccessfulRequests = g.Count(ae => ae.StatusCode >= 200 && ae.StatusCode < 300),
                    FailedRequests = g.Count(ae => ae.StatusCode >= 400),
                    AverageLatencyMs = (long)g.Average(ae => ae.LatencyMs),
                    MaxLatencyMs = g.Max(ae => ae.LatencyMs),
                    UniqueUsers = g.Where(ae => ae.UserId != null).Select(ae => ae.UserId).Distinct().Count(),
                    UniqueGuests = g.Where(ae => ae.GuestId != null).Select(ae => ae.GuestId).Distinct().Count()
                })
                .FirstOrDefaultAsync() ?? new AuditStatsDto { Period = "Summary" };

            summary.SuccessRate = summary.TotalRequests > 0 
                ? (double)summary.SuccessfulRequests / summary.TotalRequests * 100 
                : 0;

            return Ok(new AuditStatsResponse
            {
                Success = true,
                Summary = summary,
                Stats = new List<AuditStatsDto> { summary }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit statistics");
            return StatusCode(500, new AuditStatsResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving audit statistics"
            });
        }
    }

    /// <summary>
    /// Get audit events by correlation ID
    /// </summary>
    [HttpGet("correlation/{correlationId}")]
    public async Task<ActionResult<AuditQueryResponse>> GetAuditEventsByCorrelation(string correlationId)
    {
        try
        {
            var events = await _context.AuditEvents
                .Where(ae => ae.CorrelationId == correlationId)
                .OrderBy(ae => ae.Timestamp)
                .Select(ae => new AuditEventDto
                {
                    Id = ae.Id,
                    CorrelationId = ae.CorrelationId,
                    UserId = ae.UserId,
                    GuestId = ae.GuestId,
                    Route = ae.Route,
                    HttpMethod = ae.HttpMethod,
                    IpAddress = ae.IpAddress,
                    UserAgent = ae.UserAgent,
                    StatusCode = ae.StatusCode,
                    LatencyMs = ae.LatencyMs,
                    ResultSummary = ae.ResultSummary,
                    ErrorMessage = ae.ErrorMessage,
                    Timestamp = ae.Timestamp,
                    UserEmail = ae.UserEmail,
                    UserRoles = ae.UserRoles,
                    RequestSize = ae.RequestSize,
                    ResponseSize = ae.ResponseSize,
                    QueryParameters = ae.QueryParameters,
                    IsSuccess = ae.StatusCode >= 200 && ae.StatusCode < 300,
                    IsSlowRequest = ae.LatencyMs > 1000,
                    UserIdentifier = ae.UserId != null ? ae.UserId.ToString()! : ae.GuestId ?? "Anonymous"
                })
                .ToListAsync();

            return Ok(new AuditQueryResponse
            {
                Success = true,
                Events = events,
                TotalCount = events.Count,
                Page = 1,
                PageSize = events.Count,
                TotalPages = 1,
                HasNextPage = false,
                HasPreviousPage = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit events by correlation ID: {CorrelationId}", correlationId);
            return StatusCode(500, new AuditQueryResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving audit events"
            });
        }
    }
}

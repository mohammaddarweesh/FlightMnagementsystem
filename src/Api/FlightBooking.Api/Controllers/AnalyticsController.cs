using FlightBooking.Application.Analytics.Interfaces;
using FlightBooking.Domain.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Api.Controllers;

/// <summary>
/// Analytics API controller providing comprehensive business intelligence and reporting capabilities
/// </summary>
/// <remarks>
/// This controller provides access to various analytics endpoints including:
/// - Revenue analytics and trends
/// - Booking status analytics
/// - Passenger demographics
/// - Route performance metrics
/// - Comprehensive dashboard data
///
/// All endpoints require authentication and return data based on the specified date ranges and filters.
/// Data is cached for performance and refreshed periodically via background jobs.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOrStaff")]
[SwaggerTag("Analytics and reporting operations for business intelligence")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICsvExportService _csvExportService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ICsvExportService csvExportService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _csvExportService = csvExportService ?? throw new ArgumentNullException(nameof(csvExportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get comprehensive analytics summary for a specified date range
    /// </summary>
    /// <param name="startDate">Start date for the analytics period (inclusive)</param>
    /// <param name="endDate">End date for the analytics period (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics summary containing revenue, bookings, demographics, and performance metrics</returns>
    /// <response code="200">Returns the analytics summary</response>
    /// <response code="400">Invalid date range provided</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="500">Internal server error</response>
    /// <example>
    /// GET /api/analytics/summary?startDate=2024-01-01&amp;endDate=2024-01-31
    /// </example>
    [HttpGet("summary")]
    [Authorize(Policy = "AdminOrStaff")]
    [SwaggerOperation(
        Summary = "Get analytics summary",
        Description = "Retrieves comprehensive analytics summary including revenue, bookings, demographics, and performance metrics for the specified date range.",
        OperationId = "GetAnalyticsSummary",
        Tags = new[] { "Analytics" }
    )]
    [ProducesResponseType(typeof(AnalyticsSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnalyticsSummary>> GetAnalyticsSummary(
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = new DateRange(startDate, endDate);
            var summary = await _analyticsService.GetAnalyticsSummaryAsync(dateRange, cancellationToken);
            
            _logger.LogInformation("Analytics summary retrieved for {StartDate} to {EndDate}", 
                startDate, endDate);
            
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid date range provided: {StartDate} to {EndDate}", startDate, endDate);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve analytics summary");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get revenue analytics with filtering options
    /// </summary>
    [HttpGet("revenue")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<IEnumerable<RevenueAnalytics>>> GetRevenueAnalytics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] string? fareClass = null,
        [FromQuery] string? airlineCode = null,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AnalyticsFilter
            {
                DateRange = new DateRange(startDate, endDate),
                Period = period,
                RouteCodes = string.IsNullOrEmpty(routeCode) ? new List<string>() : new List<string> { routeCode },
                FareClasses = string.IsNullOrEmpty(fareClass) ? new List<string>() : new List<string> { fareClass },
                AirlineCodes = string.IsNullOrEmpty(airlineCode) ? new List<string>() : new List<string> { airlineCode }
            };

            var analytics = await _analyticsService.GetRevenueAnalyticsAsync(filter, cancellationToken);
            
            _logger.LogInformation("Revenue analytics retrieved for {StartDate} to {EndDate}", 
                startDate, endDate);
            
            return Ok(analytics);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters provided for revenue analytics");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve revenue analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get revenue breakdown for a specific period
    /// </summary>
    [HttpGet("revenue/breakdown")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<RevenueBreakdown>> GetRevenueBreakdown(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] string? fareClass = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = new DateRange(startDate, endDate);
            var breakdown = await _analyticsService.GetRevenueBreakdownAsync(
                dateRange, routeCode, fareClass, cancellationToken);
            
            return Ok(breakdown);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve revenue breakdown");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get booking status analytics
    /// </summary>
    [HttpGet("bookings")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<IEnumerable<BookingStatusAnalytics>>> GetBookingStatusAnalytics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] string? fareClass = null,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AnalyticsFilter
            {
                DateRange = new DateRange(startDate, endDate),
                Period = period,
                RouteCodes = string.IsNullOrEmpty(routeCode) ? new List<string>() : new List<string> { routeCode },
                FareClasses = string.IsNullOrEmpty(fareClass) ? new List<string>() : new List<string> { fareClass }
            };

            var analytics = await _analyticsService.GetBookingStatusAnalyticsAsync(filter, cancellationToken);
            
            return Ok(analytics);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve booking status analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get booking status summary
    /// </summary>
    [HttpGet("bookings/summary")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<Dictionary<string, int>>> GetBookingStatusSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = new DateRange(startDate, endDate);
            var summary = await _analyticsService.GetBookingStatusSummaryAsync(dateRange, cancellationToken);
            
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve booking status summary");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get passenger demographics analytics
    /// </summary>
    [HttpGet("demographics")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<IEnumerable<PassengerDemographicsAnalytics>>> GetPassengerDemographics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] string? fareClass = null,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AnalyticsFilter
            {
                DateRange = new DateRange(startDate, endDate),
                Period = period,
                RouteCodes = string.IsNullOrEmpty(routeCode) ? new List<string>() : new List<string> { routeCode },
                FareClasses = string.IsNullOrEmpty(fareClass) ? new List<string>() : new List<string> { fareClass }
            };

            var analytics = await _analyticsService.GetPassengerDemographicsAsync(filter, cancellationToken);
            
            return Ok(analytics);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve passenger demographics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get demographics breakdown
    /// </summary>
    [HttpGet("demographics/breakdown")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<DemographicsBreakdown>> GetDemographicsBreakdown(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = new DateRange(startDate, endDate);
            var breakdown = await _analyticsService.GetDemographicsBreakdownAsync(
                dateRange, routeCode, cancellationToken);
            
            return Ok(breakdown);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve demographics breakdown");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get route performance analytics
    /// </summary>
    [HttpGet("routes")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<IEnumerable<RoutePerformanceAnalytics>>> GetRoutePerformance(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AnalyticsFilter
            {
                DateRange = new DateRange(startDate, endDate),
                Period = period,
                RouteCodes = string.IsNullOrEmpty(routeCode) ? new List<string>() : new List<string> { routeCode }
            };

            var analytics = await _analyticsService.GetRoutePerformanceAsync(filter, cancellationToken);
            
            return Ok(analytics);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve route performance analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get top performing routes
    /// </summary>
    [HttpGet("routes/top")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<IEnumerable<RoutePerformanceAnalytics>>> GetTopPerformingRoutes(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int topCount = 10,
        [FromQuery] string orderBy = "TotalRevenue",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = new DateRange(startDate, endDate);
            var topRoutes = await _analyticsService.GetTopPerformingRoutesAsync(
                dateRange, topCount, orderBy, cancellationToken);
            
            return Ok(topRoutes);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top performing routes");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get revenue trend data
    /// </summary>
    [HttpGet("trends/revenue")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<IEnumerable<object>>> GetRevenueTrend(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = new DateRange(startDate, endDate);
            var trend = await _analyticsService.GetRevenueTrendAsync(dateRange, period, cancellationToken);
            
            var result = trend.Select(t => new { Date = t.Date, Revenue = t.Revenue });
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve revenue trend");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get booking trend data
    /// </summary>
    [HttpGet("trends/bookings")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<IEnumerable<object>>> GetBookingTrend(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = new DateRange(startDate, endDate);
            var trend = await _analyticsService.GetBookingTrendAsync(dateRange, period, cancellationToken);
            
            var result = trend.Select(t => new { Date = t.Date, Bookings = t.Bookings });
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve booking trend");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Compare revenue between two periods
    /// </summary>
    [HttpGet("compare/revenue")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<Dictionary<string, decimal>>> CompareRevenue(
        [FromQuery] DateTime currentStartDate,
        [FromQuery] DateTime currentEndDate,
        [FromQuery] DateTime previousStartDate,
        [FromQuery] DateTime previousEndDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentPeriod = new DateRange(currentStartDate, currentEndDate);
            var previousPeriod = new DateRange(previousStartDate, previousEndDate);
            
            var comparison = await _analyticsService.GetRevenueComparisonAsync(
                currentPeriod, previousPeriod, cancellationToken);
            
            return Ok(comparison);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare revenue");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get data quality metrics
    /// </summary>
    [HttpGet("data-quality")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<Dictionary<string, object>>> GetDataQualityMetrics(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _analyticsService.GetDataQualityMetricsAsync(cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve data quality metrics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

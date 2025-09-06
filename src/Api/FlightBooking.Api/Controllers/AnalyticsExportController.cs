using FlightBooking.Application.Analytics.Interfaces;
using FlightBooking.Domain.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Api.Controllers;

/// <summary>
/// Analytics export controller for CSV and other format exports
/// </summary>
[ApiController]
[Route("api/analytics/export")]
[Authorize(Policy = "AdminOrStaff")]
public class AnalyticsExportController : ControllerBase
{
    private readonly ICsvExportService _csvExportService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsExportController> _logger;

    public AnalyticsExportController(
        ICsvExportService csvExportService,
        IAnalyticsService analyticsService,
        ILogger<AnalyticsExportController> logger)
    {
        _csvExportService = csvExportService ?? throw new ArgumentNullException(nameof(csvExportService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Export revenue analytics to CSV
    /// </summary>
    [HttpGet("revenue/csv")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<IActionResult> ExportRevenueAnalyticsCsv(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] string? fareClass = null,
        [FromQuery] string? airlineCode = null,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        [FromQuery] bool includeHeaders = true,
        [FromQuery] bool includeMetadata = true,
        [FromQuery] string delimiter = ",",
        [FromQuery] int maxRows = 100000,
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

            var config = new ExportConfiguration
            {
                Format = ExportFormat.CSV,
                IncludeHeaders = includeHeaders,
                IncludeMetadata = includeMetadata,
                Delimiter = delimiter,
                MaxRows = maxRows,
                FileName = $"revenue_analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv"
            };

            var csvData = await _csvExportService.ExportRevenueAnalyticsAsync(filter, config, cancellationToken);
            
            _logger.LogInformation("Revenue analytics CSV exported for {StartDate} to {EndDate}, Size: {Size} bytes", 
                startDate, endDate, csvData.Length);

            return File(csvData, "text/csv", config.FileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for revenue analytics CSV export");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export revenue analytics CSV");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Export booking status analytics to CSV
    /// </summary>
    [HttpGet("bookings/csv")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<IActionResult> ExportBookingStatusAnalyticsCsv(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] string? fareClass = null,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        [FromQuery] bool includeHeaders = true,
        [FromQuery] bool includeMetadata = true,
        [FromQuery] string delimiter = ",",
        [FromQuery] int maxRows = 100000,
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

            var config = new ExportConfiguration
            {
                Format = ExportFormat.CSV,
                IncludeHeaders = includeHeaders,
                IncludeMetadata = includeMetadata,
                Delimiter = delimiter,
                MaxRows = maxRows,
                FileName = $"booking_status_analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv"
            };

            var csvData = await _csvExportService.ExportBookingStatusAnalyticsAsync(filter, config, cancellationToken);
            
            _logger.LogInformation("Booking status analytics CSV exported for {StartDate} to {EndDate}, Size: {Size} bytes", 
                startDate, endDate, csvData.Length);

            return File(csvData, "text/csv", config.FileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for booking status analytics CSV export");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export booking status analytics CSV");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Export passenger demographics to CSV
    /// </summary>
    [HttpGet("demographics/csv")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<IActionResult> ExportPassengerDemographicsCsv(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] string? fareClass = null,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        [FromQuery] bool includeHeaders = true,
        [FromQuery] bool includeMetadata = true,
        [FromQuery] string delimiter = ",",
        [FromQuery] int maxRows = 100000,
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

            var config = new ExportConfiguration
            {
                Format = ExportFormat.CSV,
                IncludeHeaders = includeHeaders,
                IncludeMetadata = includeMetadata,
                Delimiter = delimiter,
                MaxRows = maxRows,
                FileName = $"passenger_demographics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv"
            };

            var csvData = await _csvExportService.ExportPassengerDemographicsAsync(filter, config, cancellationToken);
            
            _logger.LogInformation("Passenger demographics CSV exported for {StartDate} to {EndDate}, Size: {Size} bytes", 
                startDate, endDate, csvData.Length);

            return File(csvData, "text/csv", config.FileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for passenger demographics CSV export");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export passenger demographics CSV");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Export route performance analytics to CSV
    /// </summary>
    [HttpGet("routes/csv")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<IActionResult> ExportRoutePerformanceCsv(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? routeCode = null,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Daily,
        [FromQuery] bool includeHeaders = true,
        [FromQuery] bool includeMetadata = true,
        [FromQuery] string delimiter = ",",
        [FromQuery] int maxRows = 100000,
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

            var config = new ExportConfiguration
            {
                Format = ExportFormat.CSV,
                IncludeHeaders = includeHeaders,
                IncludeMetadata = includeMetadata,
                Delimiter = delimiter,
                MaxRows = maxRows,
                FileName = $"route_performance_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv"
            };

            var csvData = await _csvExportService.ExportRoutePerformanceAsync(filter, config, cancellationToken);
            
            _logger.LogInformation("Route performance CSV exported for {StartDate} to {EndDate}, Size: {Size} bytes", 
                startDate, endDate, csvData.Length);

            return File(csvData, "text/csv", config.FileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for route performance CSV export");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export route performance CSV");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Export analytics summary to CSV
    /// </summary>
    [HttpGet("summary/csv")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<IActionResult> ExportAnalyticsSummaryCsv(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] bool includeMetadata = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = new DateRange(startDate, endDate);
            
            var config = new ExportConfiguration
            {
                Format = ExportFormat.CSV,
                IncludeHeaders = true,
                IncludeMetadata = includeMetadata,
                FileName = $"analytics_summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv"
            };

            var csvData = await _csvExportService.ExportAnalyticsSummaryAsync(dateRange, config, cancellationToken);
            
            _logger.LogInformation("Analytics summary CSV exported for {StartDate} to {EndDate}, Size: {Size} bytes", 
                startDate, endDate, csvData.Length);

            return File(csvData, "text/csv", config.FileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for analytics summary CSV export");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export analytics summary CSV");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get export URL for deferred download
    /// </summary>
    [HttpPost("url")]
    [Authorize(Policy = "AdminOrStaff")]
    public async Task<ActionResult<object>> GetExportUrl(
        [FromBody] ExportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AnalyticsFilter
            {
                DateRange = new DateRange(request.StartDate, request.EndDate),
                Period = request.Period,
                RouteCodes = request.RouteCodes ?? new List<string>(),
                FareClasses = request.FareClasses ?? new List<string>(),
                AirlineCodes = request.AirlineCodes ?? new List<string>()
            };

            var config = new ExportConfiguration
            {
                Format = request.Format,
                IncludeHeaders = request.IncludeHeaders,
                IncludeMetadata = request.IncludeMetadata,
                Delimiter = request.Delimiter,
                MaxRows = request.MaxRows,
                FileName = request.FileName ?? "export.csv"
            };

            var url = await _csvExportService.GetExportUrlAsync(
                request.ExportType, filter, config, cancellationToken);
            
            return Ok(new { exportUrl = url, expiresAt = DateTime.UtcNow.AddHours(24) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate export URL");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Bulk export multiple analytics types
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> BulkExport(
        [FromBody] BulkExportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AnalyticsFilter
            {
                DateRange = new DateRange(request.StartDate, request.EndDate),
                Period = request.Period,
                RouteCodes = request.RouteCodes ?? new List<string>(),
                FareClasses = request.FareClasses ?? new List<string>(),
                AirlineCodes = request.AirlineCodes ?? new List<string>()
            };

            var config = new ExportConfiguration
            {
                Format = ExportFormat.CSV,
                IncludeHeaders = true,
                IncludeMetadata = true,
                MaxRows = request.MaxRows
            };

            var exports = new Dictionary<string, byte[]>();

            if (request.ExportTypes.Contains("revenue"))
            {
                config.FileName = $"revenue_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.csv";
                exports["revenue"] = await _csvExportService.ExportRevenueAnalyticsAsync(filter, config, cancellationToken);
            }

            if (request.ExportTypes.Contains("bookings"))
            {
                config.FileName = $"bookings_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.csv";
                exports["bookings"] = await _csvExportService.ExportBookingStatusAnalyticsAsync(filter, config, cancellationToken);
            }

            if (request.ExportTypes.Contains("demographics"))
            {
                config.FileName = $"demographics_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.csv";
                exports["demographics"] = await _csvExportService.ExportPassengerDemographicsAsync(filter, config, cancellationToken);
            }

            if (request.ExportTypes.Contains("routes"))
            {
                config.FileName = $"routes_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.csv";
                exports["routes"] = await _csvExportService.ExportRoutePerformanceAsync(filter, config, cancellationToken);
            }

            // Create a ZIP file containing all exports
            using var memoryStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                foreach (var export in exports)
                {
                    var entry = archive.CreateEntry($"{export.Key}.csv");
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(export.Value, cancellationToken);
                }
            }

            var zipFileName = $"analytics_bulk_export_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.zip";
            
            _logger.LogInformation("Bulk export completed for {StartDate} to {EndDate}, Types: {Types}, Size: {Size} bytes", 
                request.StartDate, request.EndDate, string.Join(", ", request.ExportTypes), memoryStream.Length);

            return File(memoryStream.ToArray(), "application/zip", zipFileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform bulk export");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Export request model
/// </summary>
public class ExportRequest
{
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    public string ExportType { get; set; } = string.Empty;
    
    public AnalyticsPeriod Period { get; set; } = AnalyticsPeriod.Daily;
    public ExportFormat Format { get; set; } = ExportFormat.CSV;
    public List<string>? RouteCodes { get; set; }
    public List<string>? FareClasses { get; set; }
    public List<string>? AirlineCodes { get; set; }
    public bool IncludeHeaders { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
    public string Delimiter { get; set; } = ",";
    public int MaxRows { get; set; } = 100000;
    public string? FileName { get; set; }
}

/// <summary>
/// Bulk export request model
/// </summary>
public class BulkExportRequest
{
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    public List<string> ExportTypes { get; set; } = new();
    
    public AnalyticsPeriod Period { get; set; } = AnalyticsPeriod.Daily;
    public List<string>? RouteCodes { get; set; }
    public List<string>? FareClasses { get; set; }
    public List<string>? AirlineCodes { get; set; }
    public int MaxRows { get; set; } = 100000;
}

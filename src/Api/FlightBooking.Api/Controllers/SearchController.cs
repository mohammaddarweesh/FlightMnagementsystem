using FlightBooking.Application.Search.Services;
using FlightBooking.Contracts.Search;
using FlightBooking.Domain.Search;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FlightBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IFlightSearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(IFlightSearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search for flights with comprehensive filtering and caching
    /// </summary>
    [HttpGet("flights")]
    [ProducesResponseType(typeof(FlightSearchResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotModified)]
    [ProducesResponseType(typeof(FlightSearchResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<FlightSearchResponse>> SearchFlights(
        [FromQuery] FlightSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var criteria = MapToCriteria(request);
            var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault()?.ToString();

            var result = await _searchService.SearchFlightsAsync(criteria, ifNoneMatch, cancellationToken);

            var response = MapToResponse(result);

            // Set ETag header
            Response.Headers.ETag = $"\"{result.ETag}\"";

            // Set cache headers
            Response.Headers.CacheControl = "public, max-age=120"; // 2 minutes
            Response.Headers.Vary = "Accept-Encoding";

            // Add custom headers for debugging
            Response.Headers["X-Cache-Status"] = result.FromCache ? "HIT" : "MISS";
            Response.Headers["X-Search-Duration"] = result.Metadata.SearchDuration.TotalMilliseconds.ToString("F0");

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid search criteria provided");
            return BadRequest(new FlightSearchResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing flight search");
            return StatusCode(500, new FlightSearchResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while searching for flights"
            });
        }
    }

    /// <summary>
    /// Get availability summary for a route over a date range
    /// </summary>
    [HttpGet("availability/route")]
    [ProducesResponseType(typeof(RouteAvailabilityResponse), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<RouteAvailabilityResponse>> GetRouteAvailability(
        [FromQuery] RouteAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _searchService.GetRouteAvailabilityAsync(
                request.DepartureAirport,
                request.ArrivalAirport,
                request.StartDate,
                request.EndDate,
                request.PassengerCount,
                cancellationToken);

            var response = new RouteAvailabilityResponse
            {
                Success = true,
                DepartureAirport = result.DepartureAirport,
                ArrivalAirport = result.ArrivalAirport,
                StartDate = result.StartDate,
                EndDate = result.EndDate,
                DailyAvailability = result.DailyAvailability.Select(d => new DailyAvailabilityDto
                {
                    Date = d.Date,
                    FlightCount = d.FlightCount,
                    AvailableFlights = d.AvailableFlights,
                    MinPrice = d.MinPrice,
                    MaxPrice = d.MaxPrice,
                    AveragePrice = d.AveragePrice,
                    TotalSeats = d.TotalSeats,
                    AvailableSeats = d.AvailableSeats
                }).ToList(),
                MinPrice = result.MinPrice,
                MaxPrice = result.MaxPrice,
                AveragePrice = result.AveragePrice,
                TotalFlights = result.TotalFlights,
                AvailableFlights = result.AvailableFlights
            };

            // Set cache headers for route availability
            Response.Headers.CacheControl = "public, max-age=300"; // 5 minutes

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route availability");
            return StatusCode(500, new RouteAvailabilityResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while getting route availability"
            });
        }
    }

    /// <summary>
    /// Get popular routes with availability and pricing
    /// </summary>
    [HttpGet("popular-routes")]
    [ProducesResponseType(typeof(PopularRoutesResponse), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PopularRoutesResponse>> GetPopularRoutes(
        [FromQuery] string? fromAirport = null,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var routes = await _searchService.GetPopularRoutesAsync(fromAirport, limit, cancellationToken);

            var response = new PopularRoutesResponse
            {
                Success = true,
                Routes = routes.Select(r => new PopularRouteDto
                {
                    DepartureAirport = r.DepartureAirport,
                    DepartureAirportName = r.DepartureAirportName,
                    ArrivalAirport = r.ArrivalAirport,
                    ArrivalAirportName = r.ArrivalAirportName,
                    RouteDisplay = $"{r.DepartureAirport} → {r.ArrivalAirport}",
                    FlightCount = r.FlightCount,
                    MinPrice = r.MinPrice,
                    AveragePrice = r.AveragePrice,
                    SearchCount = r.SearchCount,
                    PopularityScore = r.PopularityScore
                }).ToList()
            };

            // Set cache headers for popular routes
            Response.Headers.CacheControl = "public, max-age=600"; // 10 minutes

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular routes");
            return StatusCode(500, new PopularRoutesResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while getting popular routes"
            });
        }
    }

    /// <summary>
    /// Get search suggestions based on partial criteria
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(SearchSuggestionsResponse), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<SearchSuggestionsResponse>> GetSearchSuggestions(
        [FromQuery] string? departureAirport = null,
        [FromQuery] string? arrivalAirport = null,
        [FromQuery] DateTime? departureDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var suggestions = await _searchService.GetSearchSuggestionsAsync(
                departureAirport, arrivalAirport, departureDate, cancellationToken);

            var response = new SearchSuggestionsResponse
            {
                Success = true,
                DepartureAirports = suggestions.DepartureAirports.Select(a => new AirportSuggestionDto
                {
                    IataCode = a.IataCode,
                    Name = a.Name,
                    City = a.City,
                    Country = a.Country,
                    FlightCount = a.FlightCount,
                    MinPrice = a.MinPrice
                }).ToList(),
                ArrivalAirports = suggestions.ArrivalAirports.Select(a => new AirportSuggestionDto
                {
                    IataCode = a.IataCode,
                    Name = a.Name,
                    City = a.City,
                    Country = a.Country,
                    FlightCount = a.FlightCount,
                    MinPrice = a.MinPrice
                }).ToList(),
                DepartureDates = suggestions.DepartureDates.Select(d => new DateSuggestionDto
                {
                    Date = d.Date,
                    MinPrice = d.MinPrice,
                    FlightCount = d.FlightCount,
                    IsWeekend = d.IsWeekend,
                    IsHoliday = d.IsHoliday
                }).ToList(),
                PopularRoutes = suggestions.PopularRoutes.Select(r => new RouteSuggestionDto
                {
                    Route = r.Route,
                    MinPrice = r.MinPrice,
                    FlightCount = r.FlightCount,
                    PopularityScore = r.PopularityScore
                }).ToList(),
                PopularDestinations = suggestions.PopularDestinations
            };

            // Set cache headers for suggestions
            Response.Headers.CacheControl = "public, max-age=1800"; // 30 minutes

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            return StatusCode(500, new SearchSuggestionsResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while getting search suggestions"
            });
        }
    }

    /// <summary>
    /// Get dynamic pricing for specific flights
    /// </summary>
    [HttpPost("pricing")]
    [ProducesResponseType(typeof(FlightPricingResponse), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<FlightPricingResponse>> GetDynamicPricing(
        [FromBody] List<Guid> flightIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pricing = await _searchService.CalculateDynamicPricingAsync(flightIds, DateTime.UtcNow, cancellationToken);

            var response = new FlightPricingResponse
            {
                Success = true,
                FlightPricing = pricing.Select(p => new FlightPricingDto
                {
                    FlightId = p.FlightId,
                    FlightNumber = p.FlightNumber,
                    DemandMultiplier = p.DemandMultiplier,
                    PricingCalculatedAt = p.PricingCalculatedAt,
                    FareClasses = p.FareClasses.Select(fc => new FareClassPricingDto
                    {
                        FareClassId = fc.FareClassId,
                        ClassName = fc.ClassName,
                        BasePrice = fc.BasePrice,
                        CurrentPrice = fc.CurrentPrice,
                        DynamicPrice = fc.DynamicPrice,
                        OccupancyRate = fc.OccupancyRate,
                        DemandScore = fc.DemandScore,
                        PricingReason = fc.PricingReason
                    }).ToList()
                }).ToList()
            };

            // Set cache headers for pricing (shorter TTL due to dynamic nature)
            Response.Headers.CacheControl = "public, max-age=60"; // 1 minute

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dynamic pricing");
            return StatusCode(500, new FlightPricingResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while calculating pricing"
            });
        }
    }

    private FlightSearchCriteria MapToCriteria(FlightSearchRequest request)
    {
        return new FlightSearchCriteria
        {
            DepartureAirport = request.DepartureAirport,
            ArrivalAirport = request.ArrivalAirport,
            DepartureDate = request.DepartureDate,
            ReturnDate = request.ReturnDate,
            PassengerCount = request.PassengerCount,
            FareClasses = request.FareClasses,
            AmenityIds = request.AmenityIds,
            MaxPrice = request.MaxPrice,
            MinPrice = request.MinPrice,
            PreferredDepartureTimeStart = request.PreferredDepartureTimeStart,
            PreferredDepartureTimeEnd = request.PreferredDepartureTimeEnd,
            Airlines = request.Airlines,
            DirectFlightsOnly = request.DirectFlightsOnly,
            MaxStops = request.MaxStops,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private FlightSearchResponse MapToResponse(FlightSearchResult result)
    {
        return new FlightSearchResponse
        {
            Success = true,
            OutboundFlights = result.OutboundFlights.Select(f => new FlightAvailabilityDto
            {
                FlightId = f.FlightId,
                FlightNumber = f.FlightNumber,
                AirlineCode = f.AirlineCode,
                AirlineName = f.AirlineName,
                AircraftType = f.AircraftType,
                DepartureAirport = f.DepartureAirport,
                ArrivalAirport = f.ArrivalAirport,
                RouteDisplay = f.RouteDisplay,
                DepartureDateTime = f.DepartureDateTime,
                ArrivalDateTime = f.ArrivalDateTime,
                FlightDuration = f.FlightDuration,
                DurationDisplay = f.DurationDisplay,
                FareClasses = f.FareClasses.Select(fc => new FareClassAvailabilityDto
                {
                    FareClassId = fc.FareClassId,
                    ClassName = fc.ClassName,
                    CurrentPrice = fc.CurrentPrice,
                    OriginalPrice = fc.OriginalPrice,
                    DiscountPercentage = fc.DiscountPercentage,
                    HasDiscount = fc.HasDiscount,
                    AvailableSeats = fc.AvailableSeats,
                    TotalSeats = fc.TotalSeats,
                    OccupancyRate = fc.OccupancyRate,
                    IncludedAmenities = fc.IncludedAmenities,
                    IsAvailable = fc.IsAvailable,
                    UnavailabilityReason = fc.UnavailabilityReason
                }).ToList(),
                AvailableAmenities = f.AvailableAmenities,
                MinPrice = f.MinPrice,
                MaxPrice = f.MaxPrice,
                TotalAvailableSeats = f.TotalAvailableSeats,
                IsInternational = f.IsInternational,
                Gate = f.Gate,
                Terminal = f.Terminal,
                LastUpdated = f.LastUpdated
            }).ToList(),
            ReturnFlights = result.ReturnFlights.Select(f => new FlightAvailabilityDto
            {
                FlightId = f.FlightId,
                FlightNumber = f.FlightNumber,
                AirlineCode = f.AirlineCode,
                AirlineName = f.AirlineName,
                AircraftType = f.AircraftType,
                DepartureAirport = f.DepartureAirport,
                ArrivalAirport = f.ArrivalAirport,
                RouteDisplay = f.RouteDisplay,
                DepartureDateTime = f.DepartureDateTime,
                ArrivalDateTime = f.ArrivalDateTime,
                FlightDuration = f.FlightDuration,
                DurationDisplay = f.DurationDisplay,
                FareClasses = f.FareClasses.Select(fc => new FareClassAvailabilityDto
                {
                    FareClassId = fc.FareClassId,
                    ClassName = fc.ClassName,
                    CurrentPrice = fc.CurrentPrice,
                    OriginalPrice = fc.OriginalPrice,
                    DiscountPercentage = fc.DiscountPercentage,
                    HasDiscount = fc.HasDiscount,
                    AvailableSeats = fc.AvailableSeats,
                    TotalSeats = fc.TotalSeats,
                    OccupancyRate = fc.OccupancyRate,
                    IncludedAmenities = fc.IncludedAmenities,
                    IsAvailable = fc.IsAvailable,
                    UnavailabilityReason = fc.UnavailabilityReason
                }).ToList(),
                AvailableAmenities = f.AvailableAmenities,
                MinPrice = f.MinPrice,
                MaxPrice = f.MaxPrice,
                TotalAvailableSeats = f.TotalAvailableSeats,
                IsInternational = f.IsInternational,
                Gate = f.Gate,
                Terminal = f.Terminal,
                LastUpdated = f.LastUpdated
            }).ToList(),
            Metadata = new SearchMetadataDto
            {
                TotalResults = result.Metadata.TotalResults,
                Page = result.Metadata.Page,
                PageSize = result.Metadata.PageSize,
                TotalPages = result.Metadata.TotalPages,
                HasNextPage = result.Metadata.HasNextPage,
                HasPreviousPage = result.Metadata.HasPreviousPage,
                MinPrice = result.Metadata.MinPrice,
                MaxPrice = result.Metadata.MaxPrice,
                AvailableAirlines = result.Metadata.AvailableAirlines,
                AvailableFareClasses = result.Metadata.AvailableFareClasses,
                AvailableAmenities = result.Metadata.AvailableAmenities.Select(a => new AmenityInfoDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Category = a.Category,
                    Icon = a.Icon,
                    FlightCount = a.FlightCount
                }).ToList(),
                ShortestDuration = result.Metadata.ShortestDuration,
                LongestDuration = result.Metadata.LongestDuration,
                SearchExecutedAt = result.Metadata.SearchExecutedAt,
                SearchDuration = result.Metadata.SearchDuration
            },
            ETag = result.ETag,
            GeneratedAt = result.GeneratedAt,
            FromCache = result.FromCache
        };
    }
}

using FlightBooking.Application.Flights.Commands;
using FlightBooking.Application.Flights.Queries;
using FlightBooking.Contracts.Common;
using FlightBooking.Contracts.Flights;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FlightsController> _logger;

    public FlightsController(IMediator mediator, ILogger<FlightsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all flights with filtering and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "StaffPolicy")]
    public async Task<ActionResult<GetFlightsResponse>> GetFlights([FromQuery] GetFlightsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get flight by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GetFlightResponse>> GetFlight(
        Guid id,
        [FromQuery] bool includeFareClasses = true,
        [FromQuery] bool includeSeats = false,
        [FromQuery] bool includeAmenities = true)
    {
        var query = new GetFlightByIdQuery
        {
            Id = id,
            IncludeFareClasses = includeFareClasses,
            IncludeSeats = includeSeats,
            IncludeAmenities = includeAmenities
        };

        var result = await _mediator.Send(query);
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Search flights for booking
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<SearchFlightsResponse>> SearchFlights([FromQuery] SearchFlightsQuery query)
    {
        if (string.IsNullOrEmpty(query.DepartureAirport) || string.IsNullOrEmpty(query.ArrivalAirport))
        {
            return BadRequest(new SearchFlightsResponse
            {
                Success = false,
                ErrorMessage = "Departure and arrival airports are required"
            });
        }

        if (query.DepartureDate.Date < DateTime.Today)
        {
            return BadRequest(new SearchFlightsResponse
            {
                Success = false,
                ErrorMessage = "Departure date cannot be in the past"
            });
        }

        if (query.Passengers < 1 || query.Passengers > 9)
        {
            return BadRequest(new SearchFlightsResponse
            {
                Success = false,
                ErrorMessage = "Number of passengers must be between 1 and 9"
            });
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new flight
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<BaseResponse>> CreateFlight([FromBody] CreateFlightCommand command)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new BaseResponse
            {
                Success = false,
                ErrorMessage = "Invalid input data",
                Data = ModelState
            });
        }

        // Validate departure date is not in the past
        if (command.DepartureDate.Date < DateTime.Today)
        {
            return BadRequest(new BaseResponse
            {
                Success = false,
                ErrorMessage = "Departure date cannot be in the past"
            });
        }

        // Validate fare classes
        if (command.FareClasses.Any())
        {
            var classNames = command.FareClasses.Select(fc => fc.ClassName.ToLower()).ToList();
            if (classNames.Count != classNames.Distinct().Count())
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Duplicate fare class names are not allowed"
                });
            }

            if (command.FareClasses.Any(fc => fc.Capacity <= 0))
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "All fare classes must have positive capacity"
                });
            }

            if (command.FareClasses.Any(fc => fc.BasePrice <= 0))
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "All fare classes must have positive base price"
                });
            }
        }

        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetFlight), new { id = result.Data }, result);
    }

    /// <summary>
    /// Update an existing flight
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<BaseResponse>> UpdateFlight(Guid id, [FromBody] UpdateFlightCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new BaseResponse
            {
                Success = false,
                ErrorMessage = "ID mismatch between route and body"
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new BaseResponse
            {
                Success = false,
                ErrorMessage = "Invalid input data",
                Data = ModelState
            });
        }

        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Delete (deactivate) a flight
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<BaseResponse>> DeleteFlight(Guid id)
    {
        var result = await _mediator.Send(new DeleteFlightCommand { Id = id });
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Get flight statistics
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Policy = "StaffPolicy")]
    public async Task<ActionResult<GetFlightStatisticsResponse>> GetFlightStatistics([FromQuery] GetFlightStatisticsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get seat map for a flight
    /// </summary>
    [HttpGet("{id}/seatmap")]
    public async Task<ActionResult<GetSeatMapResponse>> GetSeatMap(
        Guid id,
        [FromQuery] Guid? fareClassId = null)
    {
        var query = new GetSeatMapQuery
        {
            FlightId = id,
            FareClassId = fareClassId
        };

        var result = await _mediator.Send(query);
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Get flights by route
    /// </summary>
    [HttpGet("route/{routeId}")]
    public async Task<ActionResult<GetFlightsResponse>> GetFlightsByRoute(
        Guid routeId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetFlightsQuery
        {
            RouteId = routeId,
            DepartureDateFrom = fromDate,
            DepartureDateTo = toDate,
            Page = page,
            PageSize = Math.Min(pageSize, 50),
            IsActive = true,
            SortBy = "DepartureDate"
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get flights by airline
    /// </summary>
    [HttpGet("airline/{airlineCode}")]
    public async Task<ActionResult<GetFlightsResponse>> GetFlightsByAirline(
        string airlineCode,
        [FromQuery] DateTime? date = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetFlightsQuery
        {
            AirlineCode = airlineCode,
            DepartureDate = date,
            Page = page,
            PageSize = Math.Min(pageSize, 50),
            IsActive = true,
            SortBy = "DepartureDate"
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

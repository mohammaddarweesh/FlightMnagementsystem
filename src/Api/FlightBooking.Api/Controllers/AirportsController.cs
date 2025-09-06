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
public class AirportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AirportsController> _logger;

    public AirportsController(IMediator mediator, ILogger<AirportsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all airports with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GetAirportsResponse>> GetAirports([FromQuery] GetAirportsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get airport by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GetAirportResponse>> GetAirport(Guid id)
    {
        var result = await _mediator.Send(new GetAirportByIdQuery { Id = id });
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Get airport by IATA code
    /// </summary>
    [HttpGet("iata/{iataCode}")]
    public async Task<ActionResult<GetAirportResponse>> GetAirportByIata(string iataCode)
    {
        var result = await _mediator.Send(new GetAirportByIataQuery { IataCode = iataCode });
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Create a new airport
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<BaseResponse>> CreateAirport([FromBody] CreateAirportCommand command)
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

        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetAirport), new { id = result.Data }, result);
    }

    /// <summary>
    /// Update an existing airport
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<BaseResponse>> UpdateAirport(Guid id, [FromBody] UpdateAirportCommand command)
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
    /// Delete (deactivate) an airport
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<BaseResponse>> DeleteAirport(Guid id)
    {
        var result = await _mediator.Send(new DeleteAirportCommand { Id = id });
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Search airports by term (name, city, IATA code)
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<GetAirportsResponse>> SearchAirports(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAirportsQuery
        {
            SearchTerm = searchTerm,
            Page = page,
            PageSize = Math.Min(pageSize, 50),
            IsActive = true
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get airports by country
    /// </summary>
    [HttpGet("country/{countryCode}")]
    public async Task<ActionResult<GetAirportsResponse>> GetAirportsByCountry(
        string countryCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAirportsQuery
        {
            CountryCode = countryCode,
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            IsActive = true,
            SortBy = "Name"
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

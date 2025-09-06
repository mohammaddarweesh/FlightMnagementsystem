using FlightBooking.Application.Flights.Queries;
using FlightBooking.Contracts.Flights;
using FlightBooking.Domain.Flights;
using FlightBooking.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Flights.Handlers;

public class GetFlightsQueryHandler : IRequestHandler<GetFlightsQuery, GetFlightsResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetFlightsQueryHandler> _logger;

    public GetFlightsQueryHandler(ApplicationDbContext context, ILogger<GetFlightsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetFlightsResponse> Handle(GetFlightsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FareClasses)
                .Include(f => f.Seats)
                .AsQueryable();

            // Apply filters
            if (request.RouteId.HasValue)
            {
                query = query.Where(f => f.RouteId == request.RouteId.Value);
            }

            if (!string.IsNullOrEmpty(request.AirlineCode))
            {
                query = query.Where(f => f.AirlineCode == request.AirlineCode.ToUpper());
            }

            if (request.DepartureDate.HasValue)
            {
                query = query.Where(f => f.DepartureDate.Date == request.DepartureDate.Value.Date);
            }

            if (request.DepartureDateFrom.HasValue)
            {
                query = query.Where(f => f.DepartureDate.Date >= request.DepartureDateFrom.Value.Date);
            }

            if (request.DepartureDateTo.HasValue)
            {
                query = query.Where(f => f.DepartureDate.Date <= request.DepartureDateTo.Value.Date);
            }

            if (!string.IsNullOrEmpty(request.FlightNumber))
            {
                query = query.Where(f => f.FlightNumber.Contains(request.FlightNumber));
            }

            if (!string.IsNullOrEmpty(request.DepartureAirport))
            {
                query = query.Where(f => f.Route.DepartureAirport.IataCode == request.DepartureAirport.ToUpper());
            }

            if (!string.IsNullOrEmpty(request.ArrivalAirport))
            {
                query = query.Where(f => f.Route.ArrivalAirport.IataCode == request.ArrivalAirport.ToUpper());
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(f => f.IsActive == request.IsActive.Value);
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "departuredate" => request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(f => f.DepartureDate).ThenByDescending(f => f.DepartureTime)
                    : query.OrderBy(f => f.DepartureDate).ThenBy(f => f.DepartureTime),
                "flightnumber" => request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(f => f.AirlineCode).ThenByDescending(f => f.FlightNumber)
                    : query.OrderBy(f => f.AirlineCode).ThenBy(f => f.FlightNumber),
                "airline" => request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(f => f.AirlineName)
                    : query.OrderBy(f => f.AirlineName),
                _ => query.OrderBy(f => f.DepartureDate).ThenBy(f => f.DepartureTime)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var pageSize = Math.Min(request.PageSize, 100);
            var skip = (request.Page - 1) * pageSize;

            var flights = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(f => new FlightDto
                {
                    Id = f.Id,
                    FlightNumber = f.FlightNumber,
                    FullFlightNumber = f.AirlineCode + f.FlightNumber,
                    AirlineCode = f.AirlineCode,
                    AirlineName = f.AirlineName,
                    AircraftType = f.AircraftType,
                    DepartureDate = f.DepartureDate,
                    DepartureTime = f.DepartureTimeSpan,
                    ArrivalTime = f.ArrivalTime,
                    Status = f.Status.ToString(),
                    Gate = f.Gate,
                    Terminal = f.Terminal,
                    IsActive = f.IsActive,
                    Notes = f.Notes,
                    DepartureAirport = f.Route.DepartureAirport.IataCode,
                    DepartureAirportName = f.Route.DepartureAirport.Name,
                    ArrivalAirport = f.Route.ArrivalAirport.IataCode,
                    ArrivalAirportName = f.Route.ArrivalAirport.Name,
                    RouteDisplay = f.Route.DepartureAirport.IataCode + " → " + f.Route.ArrivalAirport.IataCode,
                    TotalSeats = f.FareClasses.Sum(fc => fc.Capacity),
                    AvailableSeats = f.Seats.Count(s => s.Status == SeatStatus.Available),
                    MinPrice = f.FareClasses.Where(fc => fc.IsActive).Min(fc => (decimal?)fc.CurrentPrice) ?? 0
                })
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new GetFlightsResponse
            {
                Success = true,
                Flights = flights,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flights");
            return new GetFlightsResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving flights"
            };
        }
    }
}

public class SearchFlightsQueryHandler : IRequestHandler<SearchFlightsQuery, SearchFlightsResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SearchFlightsQueryHandler> _logger;

    public SearchFlightsQueryHandler(ApplicationDbContext context, ILogger<SearchFlightsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SearchFlightsResponse> Handle(SearchFlightsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FareClasses.Where(fc => fc.IsActive))
                .Where(f => f.IsActive && 
                           f.Route.DepartureAirport.IataCode == request.DepartureAirport.ToUpper() &&
                           f.Route.ArrivalAirport.IataCode == request.ArrivalAirport.ToUpper() &&
                           f.DepartureDate.Date == request.DepartureDate.Date);

            // Apply additional filters
            if (!string.IsNullOrEmpty(request.AirlineCode))
            {
                query = query.Where(f => f.AirlineCode == request.AirlineCode.ToUpper());
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(f => f.FareClasses.Any(fc => fc.CurrentPrice <= request.MaxPrice.Value));
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "price" => request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(f => f.FareClasses.Min(fc => fc.CurrentPrice))
                    : query.OrderBy(f => f.FareClasses.Min(fc => fc.CurrentPrice)),
                "departuretime" => request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(f => f.DepartureTime)
                    : query.OrderBy(f => f.DepartureTime),
                _ => query.OrderBy(f => f.FareClasses.Min(fc => fc.CurrentPrice))
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var pageSize = Math.Min(request.PageSize, 50);
            var skip = (request.Page - 1) * pageSize;

            var flights = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(f => new FlightSearchResultDto
                {
                    Id = f.Id,
                    FullFlightNumber = f.AirlineCode + f.FlightNumber,
                    AirlineName = f.AirlineName,
                    AircraftType = f.AircraftType,
                    DepartureDateTime = f.DepartureDate.Date.Add(f.DepartureTimeSpan),
                    ArrivalDateTime = f.DepartureDate.Date.Add(f.ArrivalTime),
                    FlightDuration = f.ArrivalTime > f.DepartureTimeSpan ? f.ArrivalTime - f.DepartureTimeSpan : f.ArrivalTime.Add(TimeSpan.FromDays(1)) - f.DepartureTimeSpan,
                    DepartureAirport = f.Route.DepartureAirport.IataCode,
                    DepartureAirportName = f.Route.DepartureAirport.Name,
                    ArrivalAirport = f.Route.ArrivalAirport.IataCode,
                    ArrivalAirportName = f.Route.ArrivalAirport.Name,
                    MinPrice = f.FareClasses.Where(fc => fc.IsActive).Min(fc => (decimal?)fc.CurrentPrice) ?? 0,
                    MaxPrice = f.FareClasses.Where(fc => fc.IsActive).Max(fc => (decimal?)fc.CurrentPrice) ?? 0,
                    AvailableSeats = f.Seats.Count(s => s.Status == SeatStatus.Available),
                    IsInternational = f.Route.IsInternational
                })
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new SearchFlightsResponse
            {
                Success = true,
                Flights = flights,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1,
                Summary = new SearchFlightsSummaryDto
                {
                    MinPrice = flights.Any() ? flights.Min(f => f.MinPrice) : 0,
                    MaxPrice = flights.Any() ? flights.Max(f => f.MaxPrice) : 0,
                    TotalAvailableSeats = flights.Sum(f => f.AvailableSeats),
                    Airlines = flights.Select(f => f.AirlineName).Distinct().ToList(),
                    AircraftTypes = flights.Select(f => f.AircraftType).Distinct().ToList()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching flights");
            return new SearchFlightsResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while searching flights"
            };
        }
    }
}

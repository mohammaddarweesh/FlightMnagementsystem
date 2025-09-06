using FlightBooking.Application.Flights.Queries;
using FlightBooking.Contracts.Flights;
using FlightBooking.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Flights.Handlers;

public class GetAirportsQueryHandler : IRequestHandler<GetAirportsQuery, GetAirportsResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetAirportsQueryHandler> _logger;

    public GetAirportsQueryHandler(ApplicationDbContext context, ILogger<GetAirportsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetAirportsResponse> Handle(GetAirportsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Airports.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(searchTerm) ||
                                        a.City.ToLower().Contains(searchTerm) ||
                                        a.IataCode.ToLower().Contains(searchTerm) ||
                                        a.Country.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(request.CountryCode))
            {
                query = query.Where(a => a.CountryCode == request.CountryCode.ToUpper());
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(a => a.IsActive == request.IsActive.Value);
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDirection?.ToLower() == "desc" 
                    ? query.OrderByDescending(a => a.Name)
                    : query.OrderBy(a => a.Name),
                "city" => request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(a => a.City)
                    : query.OrderBy(a => a.City),
                "country" => request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(a => a.Country)
                    : query.OrderBy(a => a.Country),
                "iatacode" => request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(a => a.IataCode)
                    : query.OrderBy(a => a.IataCode),
                _ => query.OrderBy(a => a.Name)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var pageSize = Math.Min(request.PageSize, 100);
            var skip = (request.Page - 1) * pageSize;

            var airports = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(a => new AirportDto
                {
                    Id = a.Id,
                    IataCode = a.IataCode,
                    IcaoCode = a.IcaoCode,
                    Name = a.Name,
                    City = a.City,
                    Country = a.Country,
                    CountryCode = a.CountryCode,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    Elevation = a.Elevation,
                    TimeZone = a.TimeZone,
                    IsActive = a.IsActive,
                    Description = a.Description,
                    Website = a.Website,
                    DisplayName = a.Name + " (" + a.IataCode + ")",
                    FullLocation = a.City + ", " + a.Country
                })
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new GetAirportsResponse
            {
                Success = true,
                Airports = airports,
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
            _logger.LogError(ex, "Error retrieving airports");
            return new GetAirportsResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving airports"
            };
        }
    }
}

public class GetAirportByIdQueryHandler : IRequestHandler<GetAirportByIdQuery, GetAirportResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetAirportByIdQueryHandler> _logger;

    public GetAirportByIdQueryHandler(ApplicationDbContext context, ILogger<GetAirportByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetAirportResponse> Handle(GetAirportByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var airport = await _context.Airports
                .Where(a => a.Id == request.Id)
                .Select(a => new AirportDto
                {
                    Id = a.Id,
                    IataCode = a.IataCode,
                    IcaoCode = a.IcaoCode,
                    Name = a.Name,
                    City = a.City,
                    Country = a.Country,
                    CountryCode = a.CountryCode,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    Elevation = a.Elevation,
                    TimeZone = a.TimeZone,
                    IsActive = a.IsActive,
                    Description = a.Description,
                    Website = a.Website,
                    DisplayName = a.Name + " (" + a.IataCode + ")",
                    FullLocation = a.City + ", " + a.Country
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (airport == null)
            {
                return new GetAirportResponse
                {
                    Success = false,
                    ErrorMessage = "Airport not found"
                };
            }

            return new GetAirportResponse
            {
                Success = true,
                Airport = airport
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving airport {Id}", request.Id);
            return new GetAirportResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving the airport"
            };
        }
    }
}

public class GetAirportByIataQueryHandler : IRequestHandler<GetAirportByIataQuery, GetAirportResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetAirportByIataQueryHandler> _logger;

    public GetAirportByIataQueryHandler(ApplicationDbContext context, ILogger<GetAirportByIataQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetAirportResponse> Handle(GetAirportByIataQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var airport = await _context.Airports
                .Where(a => a.IataCode == request.IataCode.ToUpper())
                .Select(a => new AirportDto
                {
                    Id = a.Id,
                    IataCode = a.IataCode,
                    IcaoCode = a.IcaoCode,
                    Name = a.Name,
                    City = a.City,
                    Country = a.Country,
                    CountryCode = a.CountryCode,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    Elevation = a.Elevation,
                    TimeZone = a.TimeZone,
                    IsActive = a.IsActive,
                    Description = a.Description,
                    Website = a.Website,
                    DisplayName = a.Name + " (" + a.IataCode + ")",
                    FullLocation = a.City + ", " + a.Country
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (airport == null)
            {
                return new GetAirportResponse
                {
                    Success = false,
                    ErrorMessage = $"Airport with IATA code '{request.IataCode}' not found"
                };
            }

            return new GetAirportResponse
            {
                Success = true,
                Airport = airport
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving airport by IATA code {IataCode}", request.IataCode);
            return new GetAirportResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving the airport"
            };
        }
    }
}

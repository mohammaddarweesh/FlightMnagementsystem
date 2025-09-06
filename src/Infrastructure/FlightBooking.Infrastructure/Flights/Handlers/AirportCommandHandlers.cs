using FlightBooking.Application.Flights.Commands;
using FlightBooking.Contracts.Common;
using FlightBooking.Domain.Flights;
using FlightBooking.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Flights.Handlers;

public class CreateAirportCommandHandler : IRequestHandler<CreateAirportCommand, BaseResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateAirportCommandHandler> _logger;

    public CreateAirportCommandHandler(ApplicationDbContext context, ILogger<CreateAirportCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(CreateAirportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate IATA code uniqueness
            var existingAirport = await _context.Airports
                .FirstOrDefaultAsync(a => a.IataCode == request.IataCode.ToUpper(), cancellationToken);

            if (existingAirport != null)
            {
                return new BaseResponse
                {
                    Success = false,
                    ErrorMessage = $"Airport with IATA code '{request.IataCode}' already exists"
                };
            }

            var airport = Airport.Create(
                request.IataCode,
                request.IcaoCode,
                request.Name,
                request.City,
                request.Country,
                request.CountryCode,
                request.Latitude,
                request.Longitude,
                request.Elevation,
                request.TimeZone);

            airport.Description = request.Description;
            airport.Website = request.Website;

            _context.Airports.Add(airport);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created airport {IataCode} - {Name}", airport.IataCode, airport.Name);

            return new BaseResponse
            {
                Success = true,
                Message = $"Airport '{airport.GetDisplayName()}' created successfully",
                Data = airport.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating airport with IATA code {IataCode}", request.IataCode);
            return new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while creating the airport"
            };
        }
    }
}

public class UpdateAirportCommandHandler : IRequestHandler<UpdateAirportCommand, BaseResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UpdateAirportCommandHandler> _logger;

    public UpdateAirportCommandHandler(ApplicationDbContext context, ILogger<UpdateAirportCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(UpdateAirportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var airport = await _context.Airports
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (airport == null)
            {
                return new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Airport not found"
                };
            }

            // Check IATA code uniqueness if changed
            if (airport.IataCode != request.IataCode.ToUpper())
            {
                var existingAirport = await _context.Airports
                    .FirstOrDefaultAsync(a => a.IataCode == request.IataCode.ToUpper() && a.Id != request.Id, cancellationToken);

                if (existingAirport != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        ErrorMessage = $"Airport with IATA code '{request.IataCode}' already exists"
                    };
                }
            }

            // Update properties
            airport.IataCode = request.IataCode.ToUpper();
            airport.IcaoCode = request.IcaoCode.ToUpper();
            airport.Name = request.Name;
            airport.City = request.City;
            airport.Country = request.Country;
            airport.CountryCode = request.CountryCode.ToUpper();
            airport.Latitude = request.Latitude;
            airport.Longitude = request.Longitude;
            airport.Elevation = request.Elevation;
            airport.TimeZone = request.TimeZone;
            airport.IsActive = request.IsActive;
            airport.Description = request.Description;
            airport.Website = request.Website;
            airport.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated airport {IataCode} - {Name}", airport.IataCode, airport.Name);

            return new BaseResponse
            {
                Success = true,
                Message = $"Airport '{airport.GetDisplayName()}' updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating airport {Id}", request.Id);
            return new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while updating the airport"
            };
        }
    }
}

public class DeleteAirportCommandHandler : IRequestHandler<DeleteAirportCommand, BaseResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeleteAirportCommandHandler> _logger;

    public DeleteAirportCommandHandler(ApplicationDbContext context, ILogger<DeleteAirportCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(DeleteAirportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var airport = await _context.Airports
                .Include(a => a.DepartureRoutes)
                .Include(a => a.ArrivalRoutes)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (airport == null)
            {
                return new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Airport not found"
                };
            }

            // Check if airport has active routes
            var hasActiveRoutes = airport.DepartureRoutes.Any(r => r.IsActive) || 
                                 airport.ArrivalRoutes.Any(r => r.IsActive);

            if (hasActiveRoutes)
            {
                return new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Cannot delete airport with active routes. Please deactivate all routes first."
                };
            }

            // Soft delete by deactivating
            airport.IsActive = false;
            airport.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deactivated airport {IataCode} - {Name}", airport.IataCode, airport.Name);

            return new BaseResponse
            {
                Success = true,
                Message = $"Airport '{airport.GetDisplayName()}' deactivated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting airport {Id}", request.Id);
            return new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while deleting the airport"
            };
        }
    }
}

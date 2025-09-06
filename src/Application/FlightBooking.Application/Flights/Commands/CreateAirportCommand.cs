using FlightBooking.Contracts.Common;
using MediatR;

namespace FlightBooking.Application.Flights.Commands;

public class CreateAirportCommand : IRequest<BaseResponse>
{
    public string IataCode { get; set; } = string.Empty;
    public string IcaoCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int Elevation { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
}

public class UpdateAirportCommand : IRequest<BaseResponse>
{
    public Guid Id { get; set; }
    public string IataCode { get; set; } = string.Empty;
    public string IcaoCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int Elevation { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
}

public class DeleteAirportCommand : IRequest<BaseResponse>
{
    public Guid Id { get; set; }
}

public class CreateRouteCommand : IRequest<BaseResponse>
{
    public Guid DepartureAirportId { get; set; }
    public Guid ArrivalAirportId { get; set; }
    public int Distance { get; set; }
    public TimeSpan EstimatedFlightTime { get; set; }
    public bool IsInternational { get; set; }
    public string? Description { get; set; }
}

public class UpdateRouteCommand : IRequest<BaseResponse>
{
    public Guid Id { get; set; }
    public Guid DepartureAirportId { get; set; }
    public Guid ArrivalAirportId { get; set; }
    public int Distance { get; set; }
    public TimeSpan EstimatedFlightTime { get; set; }
    public bool IsInternational { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

public class DeleteRouteCommand : IRequest<BaseResponse>
{
    public Guid Id { get; set; }
}

public class CreateFlightCommand : IRequest<BaseResponse>
{
    public string FlightNumber { get; set; } = string.Empty;
    public Guid RouteId { get; set; }
    public string AirlineCode { get; set; } = string.Empty;
    public string AirlineName { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public string? Notes { get; set; }
    public List<CreateFareClassRequest> FareClasses { get; set; } = new();
}

public class CreateFareClassRequest
{
    public string ClassName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public List<Guid> AmenityIds { get; set; } = new();
}

public class UpdateFlightCommand : IRequest<BaseResponse>
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public Guid RouteId { get; set; }
    public string AirlineCode { get; set; } = string.Empty;
    public string AirlineName { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class DeleteFlightCommand : IRequest<BaseResponse>
{
    public Guid Id { get; set; }
}

public class CreateFareClassCommand : IRequest<BaseResponse>
{
    public Guid FlightId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public List<Guid> AmenityIds { get; set; } = new();
}

public class UpdateFareClassCommand : IRequest<BaseResponse>
{
    public Guid Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public List<Guid> AmenityIds { get; set; } = new();
}

public class DeleteFareClassCommand : IRequest<BaseResponse>
{
    public Guid Id { get; set; }
}

public class AttachAmenityToFareClassCommand : IRequest<BaseResponse>
{
    public Guid FareClassId { get; set; }
    public Guid AmenityId { get; set; }
    public bool IsIncluded { get; set; } = true;
    public decimal? AdditionalCost { get; set; }
    public string? Notes { get; set; }
}

public class DetachAmenityFromFareClassCommand : IRequest<BaseResponse>
{
    public Guid FareClassId { get; set; }
    public Guid AmenityId { get; set; }
}

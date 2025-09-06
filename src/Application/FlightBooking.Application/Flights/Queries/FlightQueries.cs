using FlightBooking.Contracts.Common;
using FlightBooking.Contracts.Flights;
using MediatR;

namespace FlightBooking.Application.Flights.Queries;

public class GetAirportsQuery : IRequest<GetAirportsResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public string? CountryCode { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "Name";
    public string? SortDirection { get; set; } = "asc";
}

public class GetAirportByIdQuery : IRequest<GetAirportResponse>
{
    public Guid Id { get; set; }
}

public class GetAirportByIataQuery : IRequest<GetAirportResponse>
{
    public string IataCode { get; set; } = string.Empty;
}

public class GetRoutesQuery : IRequest<GetRoutesResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public Guid? DepartureAirportId { get; set; }
    public Guid? ArrivalAirportId { get; set; }
    public bool? IsInternational { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "RouteCode";
    public string? SortDirection { get; set; } = "asc";
}

public class GetRouteByIdQuery : IRequest<GetRouteResponse>
{
    public Guid Id { get; set; }
}

public class GetFlightsQuery : IRequest<GetFlightsResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public Guid? RouteId { get; set; }
    public string? AirlineCode { get; set; }
    public DateTime? DepartureDate { get; set; }
    public DateTime? DepartureDateFrom { get; set; }
    public DateTime? DepartureDateTo { get; set; }
    public string? FlightNumber { get; set; }
    public string? DepartureAirport { get; set; }
    public string? ArrivalAirport { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "DepartureDate";
    public string? SortDirection { get; set; } = "asc";
}

public class GetFlightByIdQuery : IRequest<GetFlightResponse>
{
    public Guid Id { get; set; }
    public bool IncludeFareClasses { get; set; } = true;
    public bool IncludeSeats { get; set; } = false;
    public bool IncludeAmenities { get; set; } = true;
}

public class SearchFlightsQuery : IRequest<SearchFlightsResponse>
{
    public string DepartureAirport { get; set; } = string.Empty; // IATA code
    public string ArrivalAirport { get; set; } = string.Empty; // IATA code
    public DateTime DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int Passengers { get; set; } = 1;
    public string? PreferredClass { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? AirlineCode { get; set; }
    public bool DirectFlightsOnly { get; set; } = false;
    public string? SortBy { get; set; } = "Price"; // Price, Duration, DepartureTime
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetFareClassesQuery : IRequest<GetFareClassesResponse>
{
    public Guid? FlightId { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeAmenities { get; set; } = true;
    public string? SortBy { get; set; } = "SortOrder";
    public string? SortDirection { get; set; } = "asc";
}

public class GetFareClassByIdQuery : IRequest<GetFareClassResponse>
{
    public Guid Id { get; set; }
    public bool IncludeAmenities { get; set; } = true;
    public bool IncludeSeats { get; set; } = false;
}

public class GetSeatsQuery : IRequest<GetSeatsResponse>
{
    public Guid? FlightId { get; set; }
    public Guid? FareClassId { get; set; }
    public string? Status { get; set; }
    public string? SeatType { get; set; }
    public bool? IsAvailable { get; set; }
    public string? SortBy { get; set; } = "SeatNumber";
    public string? SortDirection { get; set; } = "asc";
}

public class GetSeatMapQuery : IRequest<GetSeatMapResponse>
{
    public Guid FlightId { get; set; }
    public Guid? FareClassId { get; set; }
}

public class GetAmenitiesQuery : IRequest<GetAmenitiesResponse>
{
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "SortOrder";
    public string? SortDirection { get; set; } = "asc";
}

public class GetAmenityByIdQuery : IRequest<GetAmenityResponse>
{
    public Guid Id { get; set; }
}

public class GetFlightStatisticsQuery : IRequest<GetFlightStatisticsResponse>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AirlineCode { get; set; }
    public Guid? RouteId { get; set; }
    public string? GroupBy { get; set; } = "day"; // day, week, month
}

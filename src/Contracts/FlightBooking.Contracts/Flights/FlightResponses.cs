using FlightBooking.Contracts.Common;

namespace FlightBooking.Contracts.Flights;

public class GetAirportsResponse : BaseResponse
{
    public List<AirportDto> Airports { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class GetAirportResponse : BaseResponse
{
    public AirportDto? Airport { get; set; }
}

public class GetRoutesResponse : BaseResponse
{
    public List<RouteDto> Routes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class GetRouteResponse : BaseResponse
{
    public RouteDto? Route { get; set; }
}

public class GetFlightsResponse : BaseResponse
{
    public List<FlightDto> Flights { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class GetFlightResponse : BaseResponse
{
    public FlightDto? Flight { get; set; }
}

public class SearchFlightsResponse : BaseResponse
{
    public List<FlightSearchResultDto> Flights { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public SearchFlightsSummaryDto Summary { get; set; } = new();
}

public class SearchFlightsSummaryDto
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public List<string> Airlines { get; set; } = new();
    public List<string> AircraftTypes { get; set; } = new();
    public int TotalAvailableSeats { get; set; }
}

public class GetFareClassesResponse : BaseResponse
{
    public List<FareClassDto> FareClasses { get; set; } = new();
}

public class GetFareClassResponse : BaseResponse
{
    public FareClassDto? FareClass { get; set; }
}

public class GetSeatsResponse : BaseResponse
{
    public List<SeatDto> Seats { get; set; } = new();
}

public class GetSeatMapResponse : BaseResponse
{
    public SeatMapDto? SeatMap { get; set; }
}

public class GetAmenitiesResponse : BaseResponse
{
    public List<AmenityDto> Amenities { get; set; } = new();
}

public class GetAmenityResponse : BaseResponse
{
    public AmenityDto? Amenity { get; set; }
}

public class GetFlightStatisticsResponse : BaseResponse
{
    public List<FlightStatisticsDto> Statistics { get; set; } = new();
    public FlightStatisticsSummaryDto Summary { get; set; } = new();
}

public class FlightStatisticsDto
{
    public string Period { get; set; } = string.Empty;
    public int TotalFlights { get; set; }
    public int ActiveFlights { get; set; }
    public int CancelledFlights { get; set; }
    public int DelayedFlights { get; set; }
    public decimal AverageOccupancyRate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalPassengers { get; set; }
    public Dictionary<string, int> FlightsByAirline { get; set; } = new();
    public Dictionary<string, int> FlightsByRoute { get; set; } = new();
}

public class FlightStatisticsSummaryDto
{
    public int TotalFlights { get; set; }
    public decimal OverallOccupancyRate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalPassengers { get; set; }
    public string MostPopularRoute { get; set; } = string.Empty;
    public string MostPopularAirline { get; set; } = string.Empty;
    public decimal AverageFlightPrice { get; set; }
}

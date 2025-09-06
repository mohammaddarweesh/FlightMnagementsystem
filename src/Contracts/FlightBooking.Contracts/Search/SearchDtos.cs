using FlightBooking.Contracts.Common;

namespace FlightBooking.Contracts.Search;

public class FlightSearchRequest
{
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int PassengerCount { get; set; } = 1;
    public List<string> FareClasses { get; set; } = new();
    public List<Guid> AmenityIds { get; set; } = new();
    public decimal? MaxPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public TimeSpan? PreferredDepartureTimeStart { get; set; }
    public TimeSpan? PreferredDepartureTimeEnd { get; set; }
    public List<string> Airlines { get; set; } = new();
    public bool DirectFlightsOnly { get; set; } = false;
    public int? MaxStops { get; set; }
    public string? SortBy { get; set; } = "price";
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class FlightSearchResponse : BaseResponse
{
    public List<FlightAvailabilityDto> OutboundFlights { get; set; } = new();
    public List<FlightAvailabilityDto> ReturnFlights { get; set; } = new();
    public SearchMetadataDto Metadata { get; set; } = new();
    public string ETag { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public bool FromCache { get; set; }
}

public class FlightAvailabilityDto
{
    public Guid FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string AirlineCode { get; set; } = string.Empty;
    public string AirlineName { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public string RouteDisplay { get; set; } = string.Empty;
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public TimeSpan FlightDuration { get; set; }
    public string DurationDisplay { get; set; } = string.Empty;
    public List<FareClassAvailabilityDto> FareClasses { get; set; } = new();
    public List<Guid> AvailableAmenities { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalAvailableSeats { get; set; }
    public bool IsInternational { get; set; }
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class FareClassAvailabilityDto
{
    public Guid FareClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool HasDiscount { get; set; }
    public int AvailableSeats { get; set; }
    public int TotalSeats { get; set; }
    public double OccupancyRate { get; set; }
    public List<Guid> IncludedAmenities { get; set; } = new();
    public bool IsAvailable { get; set; }
    public string? UnavailabilityReason { get; set; }
}

public class SearchMetadataDto
{
    public int TotalResults { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<string> AvailableAirlines { get; set; } = new();
    public List<string> AvailableFareClasses { get; set; } = new();
    public List<AmenityInfoDto> AvailableAmenities { get; set; } = new();
    public TimeSpan? ShortestDuration { get; set; }
    public TimeSpan? LongestDuration { get; set; }
    public DateTime SearchExecutedAt { get; set; }
    public TimeSpan SearchDuration { get; set; }
}

public class AmenityInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int FlightCount { get; set; }
}

public class RouteAvailabilityRequest
{
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int PassengerCount { get; set; } = 1;
}

public class RouteAvailabilityResponse : BaseResponse
{
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DailyAvailabilityDto> DailyAvailability { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public int TotalFlights { get; set; }
    public int AvailableFlights { get; set; }
}

public class DailyAvailabilityDto
{
    public DateTime Date { get; set; }
    public int FlightCount { get; set; }
    public int AvailableFlights { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
}

public class PopularRoutesResponse : BaseResponse
{
    public List<PopularRouteDto> Routes { get; set; } = new();
}

public class PopularRouteDto
{
    public string DepartureAirport { get; set; } = string.Empty;
    public string DepartureAirportName { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public string ArrivalAirportName { get; set; } = string.Empty;
    public string RouteDisplay { get; set; } = string.Empty;
    public int FlightCount { get; set; }
    public decimal MinPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public int SearchCount { get; set; }
    public double PopularityScore { get; set; }
}

public class SearchSuggestionsResponse : BaseResponse
{
    public List<AirportSuggestionDto> DepartureAirports { get; set; } = new();
    public List<AirportSuggestionDto> ArrivalAirports { get; set; } = new();
    public List<DateSuggestionDto> DepartureDates { get; set; } = new();
    public List<RouteSuggestionDto> PopularRoutes { get; set; } = new();
    public List<string> PopularDestinations { get; set; } = new();
}

public class AirportSuggestionDto
{
    public string IataCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int FlightCount { get; set; }
    public decimal MinPrice { get; set; }
}

public class DateSuggestionDto
{
    public DateTime Date { get; set; }
    public decimal MinPrice { get; set; }
    public int FlightCount { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsHoliday { get; set; }
}

public class RouteSuggestionDto
{
    public string Route { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public int FlightCount { get; set; }
    public double PopularityScore { get; set; }
}

public class FlightPricingResponse : BaseResponse
{
    public List<FlightPricingDto> FlightPricing { get; set; } = new();
}

public class FlightPricingDto
{
    public Guid FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public List<FareClassPricingDto> FareClasses { get; set; } = new();
    public decimal DemandMultiplier { get; set; }
    public DateTime PricingCalculatedAt { get; set; }
}

public class FareClassPricingDto
{
    public Guid FareClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DynamicPrice { get; set; }
    public double OccupancyRate { get; set; }
    public double DemandScore { get; set; }
    public string PricingReason { get; set; } = string.Empty;
}

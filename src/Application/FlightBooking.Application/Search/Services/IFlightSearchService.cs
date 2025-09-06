using FlightBooking.Domain.Search;

namespace FlightBooking.Application.Search.Services;

public interface IFlightSearchService
{
    /// <summary>
    /// Searches for flights based on criteria with caching support
    /// </summary>
    Task<FlightSearchResult> SearchFlightsAsync(
        FlightSearchCriteria criteria, 
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets real-time availability for specific flights
    /// </summary>
    Task<List<FlightAvailability>> GetFlightAvailabilityAsync(
        List<Guid> flightIds,
        int passengerCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets availability summary for a route and date range
    /// </summary>
    Task<RouteAvailabilitySummary> GetRouteAvailabilityAsync(
        string departureAirport,
        string arrivalAirport,
        DateTime startDate,
        DateTime endDate,
        int passengerCount = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets popular routes with availability
    /// </summary>
    Task<List<PopularRoute>> GetPopularRoutesAsync(
        string? fromAirport = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets flight search suggestions based on partial criteria
    /// </summary>
    Task<FlightSearchSuggestions> GetSearchSuggestionsAsync(
        string? departureAirport = null,
        string? arrivalAirport = null,
        DateTime? departureDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates dynamic pricing for flights based on demand
    /// </summary>
    Task<List<FlightPricing>> CalculateDynamicPricingAsync(
        List<Guid> flightIds,
        DateTime searchDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates search cache for specific criteria
    /// </summary>
    Task InvalidateSearchCacheAsync(
        string? departureAirport = null,
        string? arrivalAirport = null,
        DateTime? date = null,
        Guid? flightId = null,
        Guid? fareClassId = null,
        CancellationToken cancellationToken = default);
}

public class RouteAvailabilitySummary
{
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DailyAvailability> DailyAvailability { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public int TotalFlights { get; set; }
    public int AvailableFlights { get; set; }
}

public class DailyAvailability
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

public class PopularRoute
{
    public string DepartureAirport { get; set; } = string.Empty;
    public string DepartureAirportName { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public string ArrivalAirportName { get; set; } = string.Empty;
    public int FlightCount { get; set; }
    public decimal MinPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public int SearchCount { get; set; }
    public double PopularityScore { get; set; }
}

public class FlightSearchSuggestions
{
    public List<AirportSuggestion> DepartureAirports { get; set; } = new();
    public List<AirportSuggestion> ArrivalAirports { get; set; } = new();
    public List<DateSuggestion> DepartureDates { get; set; } = new();
    public List<RouteSuggestion> PopularRoutes { get; set; } = new();
    public List<string> PopularDestinations { get; set; } = new();
}

public class AirportSuggestion
{
    public string IataCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int FlightCount { get; set; }
    public decimal MinPrice { get; set; }
}

public class DateSuggestion
{
    public DateTime Date { get; set; }
    public decimal MinPrice { get; set; }
    public int FlightCount { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsHoliday { get; set; }
}

public class RouteSuggestion
{
    public string Route { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public int FlightCount { get; set; }
    public double PopularityScore { get; set; }
}

public class FlightPricing
{
    public Guid FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public List<FareClassPricing> FareClasses { get; set; } = new();
    public decimal DemandMultiplier { get; set; }
    public DateTime PricingCalculatedAt { get; set; }
}

public class FareClassPricing
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

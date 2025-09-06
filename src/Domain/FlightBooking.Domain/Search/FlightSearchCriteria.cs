using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Search;

public class FlightSearchCriteria : BaseEntity
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
    public string? SortBy { get; set; } = "price"; // price, duration, departure_time, arrival_time
    public string? SortDirection { get; set; } = "asc"; // asc, desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Computed properties
    public bool IsRoundTrip => ReturnDate.HasValue;
    public string RouteKey => $"{DepartureAirport}-{ArrivalAirport}";
    public string DateRangeKey => IsRoundTrip ? $"{DepartureDate:yyyy-MM-dd}_{ReturnDate:yyyy-MM-dd}" : DepartureDate.ToString("yyyy-MM-dd");
    
    public string GetCacheKey()
    {
        var keyParts = new List<string>
        {
            "flight_search",
            RouteKey,
            DateRangeKey,
            PassengerCount.ToString(),
            string.Join(",", FareClasses.OrderBy(x => x)),
            string.Join(",", AmenityIds.OrderBy(x => x)),
            MaxPrice?.ToString("F2") ?? "null",
            MinPrice?.ToString("F2") ?? "null",
            PreferredDepartureTimeStart?.ToString(@"hh\:mm") ?? "null",
            PreferredDepartureTimeEnd?.ToString(@"hh\:mm") ?? "null",
            string.Join(",", Airlines.OrderBy(x => x)),
            DirectFlightsOnly.ToString().ToLower(),
            MaxStops?.ToString() ?? "null",
            SortBy ?? "price",
            SortDirection ?? "asc",
            Page.ToString(),
            PageSize.ToString()
        };

        return string.Join(":", keyParts);
    }

    public string GetAvailabilityCacheKey()
    {
        // Simpler key for availability data (without pagination/sorting)
        var keyParts = new List<string>
        {
            "availability",
            RouteKey,
            DateRangeKey,
            PassengerCount.ToString(),
            string.Join(",", FareClasses.OrderBy(x => x))
        };

        return string.Join(":", keyParts);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DepartureAirport))
            throw new ArgumentException("Departure airport is required");

        if (string.IsNullOrWhiteSpace(ArrivalAirport))
            throw new ArgumentException("Arrival airport is required");

        if (DepartureDate < DateTime.Today)
            throw new ArgumentException("Departure date cannot be in the past");

        if (IsRoundTrip && ReturnDate < DepartureDate)
            throw new ArgumentException("Return date must be after departure date");

        if (PassengerCount < 1 || PassengerCount > 9)
            throw new ArgumentException("Passenger count must be between 1 and 9");

        if (Page < 1)
            throw new ArgumentException("Page must be greater than 0");

        if (PageSize < 1 || PageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100");

        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            throw new ArgumentException("Minimum price cannot be greater than maximum price");

        if (PreferredDepartureTimeStart.HasValue && PreferredDepartureTimeEnd.HasValue && 
            PreferredDepartureTimeStart > PreferredDepartureTimeEnd)
            throw new ArgumentException("Preferred departure time start cannot be after end time");
    }
}

public class FlightAvailability : BaseEntity
{
    public Guid FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string AirlineCode { get; set; } = string.Empty;
    public string AirlineName { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public TimeSpan FlightDuration { get; set; }
    public List<FareClassAvailability> FareClasses { get; set; } = new();
    public List<Guid> AvailableAmenities { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalAvailableSeats { get; set; }
    public bool IsInternational { get; set; }
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public DateTime LastUpdated { get; set; }

    // Computed properties
    public bool HasAvailability => TotalAvailableSeats > 0;
    public string RouteDisplay => $"{DepartureAirport} → {ArrivalAirport}";
    public string DurationDisplay => $"{FlightDuration.Hours}h {FlightDuration.Minutes}m";
}

public class FareClassAvailability
{
    public Guid FareClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public int AvailableSeats { get; set; }
    public int TotalSeats { get; set; }
    public List<Guid> IncludedAmenities { get; set; } = new();
    public bool IsAvailable { get; set; }
    public string? UnavailabilityReason { get; set; }

    // Computed properties
    public decimal DiscountPercentage => OriginalPrice > 0 ? ((OriginalPrice - CurrentPrice) / OriginalPrice) * 100 : 0;
    public bool HasDiscount => DiscountPercentage > 0;
    public double OccupancyRate => TotalSeats > 0 ? (double)(TotalSeats - AvailableSeats) / TotalSeats : 0;
}

public class FlightSearchResult
{
    public List<FlightAvailability> OutboundFlights { get; set; } = new();
    public List<FlightAvailability> ReturnFlights { get; set; } = new();
    public FlightSearchMetadata Metadata { get; set; } = new();
    public string ETag { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public bool FromCache { get; set; }
}

public class FlightSearchMetadata
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
    public List<AmenityInfo> AvailableAmenities { get; set; } = new();
    public TimeSpan? ShortestDuration { get; set; }
    public TimeSpan? LongestDuration { get; set; }
    public DateTime SearchExecutedAt { get; set; }
    public TimeSpan SearchDuration { get; set; }
}

public class AmenityInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int FlightCount { get; set; }
}

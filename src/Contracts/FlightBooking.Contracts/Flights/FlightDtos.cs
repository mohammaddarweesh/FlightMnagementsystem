using FlightBooking.Contracts.Common;

namespace FlightBooking.Contracts.Flights;

public class AirportDto
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
    public string DisplayName { get; set; } = string.Empty;
    public string FullLocation { get; set; } = string.Empty;
}

public class RouteDto
{
    public Guid Id { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public int Distance { get; set; }
    public TimeSpan EstimatedFlightTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsInternational { get; set; }
    public string? Description { get; set; }
    public AirportDto DepartureAirport { get; set; } = null!;
    public AirportDto ArrivalAirport { get; set; } = null!;
    public string RouteDisplay { get; set; } = string.Empty;
    public string RouteDescription { get; set; } = string.Empty;
}

public class FlightDto
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string FullFlightNumber { get; set; } = string.Empty;
    public string AirlineCode { get; set; } = string.Empty;
    public string AirlineName { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string DepartureAirport { get; set; } = string.Empty;
    public string DepartureAirportName { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public string ArrivalAirportName { get; set; } = string.Empty;
    public string RouteDisplay { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal MinPrice { get; set; }
    public List<FareClassDto> FareClasses { get; set; } = new();
    public DateTime DepartureDateTime => DepartureDate.Date.Add(DepartureTime);
    public DateTime ArrivalDateTime => DepartureDate.Date.Add(ArrivalTime);
    public TimeSpan FlightDuration => ArrivalTime > DepartureTime ? ArrivalTime - DepartureTime : ArrivalTime.Add(TimeSpan.FromDays(1)) - DepartureTime;
}

public class FareClassDto
{
    public Guid Id { get; set; }
    public Guid FlightId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int AvailableSeats { get; set; }
    public int BookedSeats { get; set; }
    public decimal OccupancyRate { get; set; }
    public bool IsSoldOut { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<AmenityDto> Amenities { get; set; } = new();
    public List<SeatDto> Seats { get; set; } = new();
}

public class SeatDto
{
    public Guid Id { get; set; }
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public string Column { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? ExtraFee { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsWindow { get; set; }
    public bool IsAisle { get; set; }
    public bool IsMiddle { get; set; }
    public bool HasExtraFee { get; set; }
    public decimal TotalPrice { get; set; }
    public string SeatDescription { get; set; } = string.Empty;
}

public class AmenityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public bool IsIncluded { get; set; }
    public decimal? AdditionalCost { get; set; }
    public string? Notes { get; set; }
}

public class FlightSearchResultDto
{
    public Guid Id { get; set; }
    public string FullFlightNumber { get; set; } = string.Empty;
    public string AirlineName { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public TimeSpan FlightDuration { get; set; }
    public string DepartureAirport { get; set; } = string.Empty;
    public string DepartureAirportName { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public string ArrivalAirportName { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int AvailableSeats { get; set; }
    public bool IsInternational { get; set; }
    public List<FareClassDto> AvailableFareClasses { get; set; } = new();
}

public class SeatMapDto
{
    public Guid FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public List<SeatMapSectionDto> Sections { get; set; } = new();
}

public class SeatMapSectionDto
{
    public string SectionName { get; set; } = string.Empty;
    public string FareClassName { get; set; } = string.Empty;
    public List<SeatMapRowDto> Rows { get; set; } = new();
}

public class SeatMapRowDto
{
    public string RowNumber { get; set; } = string.Empty;
    public List<SeatDto> Seats { get; set; } = new();
}

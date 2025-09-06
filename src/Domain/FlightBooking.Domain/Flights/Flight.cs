using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Flights;

public class Flight : BaseEntity
{
    public string FlightNumber { get; set; } = string.Empty;
    public Guid RouteId { get; set; }
    public string AirlineCode { get; set; } = string.Empty; // e.g., "AA", "DL", "UA"
    public string AirlineName { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty; // e.g., "Boeing 737-800"
    public DateTime DepartureDate { get; set; }
    public TimeSpan DepartureTimeSpan { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public int TotalSeats { get; set; }
    public DateTime DepartureTime { get; set; }

    // Navigation properties
    public virtual ICollection<Domain.Bookings.Booking> Bookings { get; set; } = new List<Domain.Bookings.Booking>();
    public virtual Route Route { get; set; } = null!;
    public virtual ICollection<FareClass> FareClasses { get; set; } = new List<FareClass>();
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    // Computed properties
    public DateTime DepartureDateTime => DepartureDate.Date.Add(DepartureTimeSpan);
    public DateTime ArrivalDateTime => DepartureDate.Date.Add(ArrivalTime);
    public string FullFlightNumber => $"{AirlineCode}{FlightNumber}";
    public bool IsInternational => Route?.IsInternational ?? false;
    public bool IsDeparted => DateTime.UtcNow > DepartureDateTime;
    public bool IsArrived => DateTime.UtcNow > ArrivalDateTime;

    // Helper methods
    public string GetFlightDisplay() => $"{FullFlightNumber} - {Route?.GetRouteDisplay()}";
    
    public TimeSpan GetFlightDuration()
    {
        var duration = ArrivalTime - DepartureTimeSpan;

        // Handle overnight flights
        if (duration < TimeSpan.Zero)
        {
            duration = duration.Add(TimeSpan.FromDays(1));
        }

        return duration;
    }

    public int GetTotalSeats()
    {
        return FareClasses.Sum(fc => fc.Capacity);
    }

    public int GetAvailableSeats()
    {
        return Seats.Count(s => s.Status == SeatStatus.Available);
    }

    public int GetBookedSeats()
    {
        return Seats.Count(s => s.Status == SeatStatus.Occupied);
    }

    public decimal GetOccupancyRate()
    {
        var totalSeats = GetTotalSeats();
        return totalSeats > 0 ? (decimal)GetBookedSeats() / totalSeats * 100 : 0;
    }

    public bool CanBeBooked()
    {
        return Status == FlightStatus.Scheduled && 
               IsActive && 
               !IsDeparted && 
               GetAvailableSeats() > 0;
    }

    public void UpdateStatus(FlightStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Flight Create(
        string flightNumber,
        Guid routeId,
        string airlineCode,
        string airlineName,
        string aircraftType,
        DateTime departureDate,
        TimeSpan departureTime,
        TimeSpan arrivalTime)
    {
        return new Flight
        {
            FlightNumber = flightNumber,
            RouteId = routeId,
            AirlineCode = airlineCode.ToUpper(),
            AirlineName = airlineName,
            AircraftType = aircraftType,
            DepartureDate = departureDate.Date,
            DepartureTimeSpan = departureTime,
            DepartureTime = departureDate.Date.Add(departureTime),
            ArrivalTime = arrivalTime,
            Status = FlightStatus.Scheduled,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum FlightStatus
{
    Scheduled = 0,
    Delayed = 1,
    Boarding = 2,
    Departed = 3,
    InFlight = 4,
    Arrived = 5,
    Cancelled = 6,
    Diverted = 7
}

using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Flights;

public class Route : BaseEntity
{
    public Guid DepartureAirportId { get; set; }
    public Guid ArrivalAirportId { get; set; }
    public string RouteCode { get; set; } = string.Empty; // e.g., "NYC-LAX"
    public int Distance { get; set; } // in kilometers
    public TimeSpan EstimatedFlightTime { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsInternational { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public virtual Airport DepartureAirport { get; set; } = null!;
    public virtual Airport ArrivalAirport { get; set; } = null!;
    public virtual ICollection<Flight> Flights { get; set; } = new List<Flight>();

    // Helper methods
    public string GetRouteDisplay() => $"{DepartureAirport?.IataCode} → {ArrivalAirport?.IataCode}";
    
    public string GetRouteDescription() => 
        $"{DepartureAirport?.GetDisplayName()} to {ArrivalAirport?.GetDisplayName()}";

    public TimeSpan GetEstimatedArrivalTime(TimeSpan departureTime)
    {
        var arrivalTime = departureTime.Add(EstimatedFlightTime);
        
        // Handle day overflow
        if (arrivalTime.TotalDays >= 1)
        {
            arrivalTime = TimeSpan.FromTicks(arrivalTime.Ticks % TimeSpan.TicksPerDay);
        }
        
        return arrivalTime;
    }

    public bool IsValidRoute()
    {
        return DepartureAirportId != ArrivalAirportId && 
               Distance > 0 && 
               EstimatedFlightTime > TimeSpan.Zero;
    }

    public static Route Create(
        Guid departureAirportId,
        Guid arrivalAirportId,
        int distance,
        TimeSpan estimatedFlightTime,
        bool isInternational = false)
    {
        var route = new Route
        {
            DepartureAirportId = departureAirportId,
            ArrivalAirportId = arrivalAirportId,
            Distance = distance,
            EstimatedFlightTime = estimatedFlightTime,
            IsInternational = isInternational,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Generate route code (will be updated when airports are loaded)
        route.RouteCode = $"{departureAirportId:N}_{arrivalAirportId:N}"[..8];

        return route;
    }

    public void UpdateRouteCode(string departureIata, string arrivalIata)
    {
        RouteCode = $"{departureIata}-{arrivalIata}";
    }
}

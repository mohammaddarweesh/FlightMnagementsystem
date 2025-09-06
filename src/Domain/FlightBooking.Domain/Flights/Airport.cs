using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Flights;

public class Airport : BaseEntity
{
    public string IataCode { get; set; } = string.Empty;
    public string IcaoCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int Elevation { get; set; } // in feet
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string? Website { get; set; }

    // Navigation properties
    public virtual ICollection<Route> DepartureRoutes { get; set; } = new List<Route>();
    public virtual ICollection<Route> ArrivalRoutes { get; set; } = new List<Route>();

    // Helper methods
    public string GetDisplayName() => $"{Name} ({IataCode})";
    
    public string GetFullLocation() => $"{City}, {Country}";
    
    public double DistanceTo(Airport other)
    {
        // Haversine formula for calculating distance between two points on Earth
        const double earthRadius = 6371; // km
        
        var lat1Rad = (double)Latitude * Math.PI / 180;
        var lat2Rad = (double)other.Latitude * Math.PI / 180;
        var deltaLatRad = ((double)other.Latitude - (double)Latitude) * Math.PI / 180;
        var deltaLonRad = ((double)other.Longitude - (double)Longitude) * Math.PI / 180;

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return earthRadius * c;
    }

    public bool IsInternational(Airport other)
    {
        return CountryCode != other.CountryCode;
    }

    public static Airport Create(
        string iataCode,
        string icaoCode,
        string name,
        string city,
        string country,
        string countryCode,
        decimal latitude,
        decimal longitude,
        int elevation,
        string timeZone)
    {
        return new Airport
        {
            IataCode = iataCode.ToUpper(),
            IcaoCode = icaoCode.ToUpper(),
            Name = name,
            City = city,
            Country = country,
            CountryCode = countryCode.ToUpper(),
            Latitude = latitude,
            Longitude = longitude,
            Elevation = elevation,
            TimeZone = timeZone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}

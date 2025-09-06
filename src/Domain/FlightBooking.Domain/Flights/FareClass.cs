using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Flights;

public class FareClass : BaseEntity
{
    public Guid FlightId { get; set; }
    public string ClassName { get; set; } = string.Empty; // Economy, Premium, Business, First
    public string ClassCode { get; set; } = string.Empty; // Y, W, J, F
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int SortOrder { get; set; } // For display ordering

    // Navigation properties
    public virtual Flight Flight { get; set; } = null!;
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public virtual ICollection<FareClassAmenity> FareClassAmenities { get; set; } = new List<FareClassAmenity>();

    // Computed properties
    public int AvailableSeats => Seats.Count(s => s.Status == SeatStatus.Available);
    public int BookedSeats => Seats.Count(s => s.Status == SeatStatus.Occupied);
    public decimal OccupancyRate => Capacity > 0 ? (decimal)BookedSeats / Capacity * 100 : 0;
    public bool IsSoldOut => AvailableSeats == 0;
    public bool IsAvailable => IsActive && !IsSoldOut;

    // Helper methods
    public string GetDisplayName() => $"{ClassName} ({ClassCode})";
    
    public decimal GetPriceWithMarkup(decimal markupPercentage = 0)
    {
        return CurrentPrice * (1 + markupPercentage / 100);
    }

    public void UpdatePrice(decimal newPrice)
    {
        CurrentPrice = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyDynamicPricing()
    {
        // Simple dynamic pricing based on occupancy
        var occupancyRate = OccupancyRate;
        var priceMultiplier = occupancyRate switch
        {
            >= 90 => 1.5m,  // 50% increase when 90%+ full
            >= 75 => 1.3m,  // 30% increase when 75%+ full
            >= 50 => 1.1m,  // 10% increase when 50%+ full
            _ => 1.0m       // Base price when less than 50% full
        };

        CurrentPrice = BasePrice * priceMultiplier;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanAccommodatePassengers(int passengerCount)
    {
        return IsAvailable && AvailableSeats >= passengerCount;
    }

    public static FareClass Create(
        Guid flightId,
        string className,
        string classCode,
        int capacity,
        decimal basePrice,
        int sortOrder = 0)
    {
        return new FareClass
        {
            FlightId = flightId,
            ClassName = className,
            ClassCode = classCode.ToUpper(),
            Capacity = capacity,
            BasePrice = basePrice,
            CurrentPrice = basePrice,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static List<FareClass> CreateStandardClasses(Guid flightId, string aircraftType)
    {
        return aircraftType.ToLower() switch
        {
            var type when type.Contains("boeing 737") => new List<FareClass>
            {
                Create(flightId, "Economy", "Y", 150, 299m, 1),
                Create(flightId, "Premium Economy", "W", 20, 499m, 2),
                Create(flightId, "Business", "J", 12, 899m, 3)
            },
            var type when type.Contains("airbus a320") => new List<FareClass>
            {
                Create(flightId, "Economy", "Y", 144, 279m, 1),
                Create(flightId, "Premium Economy", "W", 24, 479m, 2),
                Create(flightId, "Business", "J", 16, 849m, 3)
            },
            var type when type.Contains("boeing 777") => new List<FareClass>
            {
                Create(flightId, "Economy", "Y", 280, 399m, 1),
                Create(flightId, "Premium Economy", "W", 40, 699m, 2),
                Create(flightId, "Business", "J", 42, 1299m, 3),
                Create(flightId, "First", "F", 8, 2499m, 4)
            },
            _ => new List<FareClass>
            {
                Create(flightId, "Economy", "Y", 100, 299m, 1),
                Create(flightId, "Business", "J", 20, 899m, 2)
            }
        };
    }
}

using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Flights;

public class Amenity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AmenityCategory Category { get; set; }
    public string? IconName { get; set; } // For UI display
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    // Navigation properties
    public virtual ICollection<FareClassAmenity> FareClassAmenities { get; set; } = new List<FareClassAmenity>();

    // Helper methods
    public string GetCategoryDisplay() => Category.ToString().Replace("_", " ");

    public static Amenity Create(
        string name,
        string description,
        AmenityCategory category,
        string? iconName = null,
        int sortOrder = 0)
    {
        return new Amenity
        {
            Name = name,
            Description = description,
            Category = category,
            IconName = iconName,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static List<Amenity> GetStandardAmenities()
    {
        return new List<Amenity>
        {
            // Seating
            Create("Extra Legroom", "Additional space for comfort", AmenityCategory.Seating, "legroom", 1),
            Create("Reclining Seats", "Seats that recline for comfort", AmenityCategory.Seating, "recline", 2),
            Create("Lie-Flat Seats", "Fully flat seats for sleeping", AmenityCategory.Seating, "bed", 3),
            Create("Priority Boarding", "Board the aircraft first", AmenityCategory.Seating, "priority", 4),

            // Food & Beverage
            Create("Complimentary Meals", "Free meals during flight", AmenityCategory.Food_Beverage, "meal", 5),
            Create("Premium Dining", "Multi-course gourmet meals", AmenityCategory.Food_Beverage, "dining", 6),
            Create("Complimentary Drinks", "Free beverages including alcohol", AmenityCategory.Food_Beverage, "drinks", 7),
            Create("Snack Service", "Light snacks and refreshments", AmenityCategory.Food_Beverage, "snack", 8),

            // Entertainment
            Create("In-Flight WiFi", "Internet access during flight", AmenityCategory.Entertainment, "wifi", 9),
            Create("Personal Entertainment", "Individual seat-back screens", AmenityCategory.Entertainment, "screen", 10),
            Create("Live TV", "Live television programming", AmenityCategory.Entertainment, "tv", 11),
            Create("Audio Entertainment", "Music and podcast selection", AmenityCategory.Entertainment, "audio", 12),

            // Comfort
            Create("Blanket & Pillow", "Comfort items for rest", AmenityCategory.Comfort, "pillow", 13),
            Create("Amenity Kit", "Personal care items", AmenityCategory.Comfort, "kit", 14),
            Create("Power Outlets", "Charging ports for devices", AmenityCategory.Comfort, "power", 15),
            Create("USB Charging", "USB ports for device charging", AmenityCategory.Comfort, "usb", 16),

            // Baggage
            Create("Free Checked Bag", "Complimentary checked baggage", AmenityCategory.Baggage, "baggage", 17),
            Create("Extra Baggage Allowance", "Additional baggage weight", AmenityCategory.Baggage, "extra-bag", 18),
            Create("Priority Baggage", "First baggage off the plane", AmenityCategory.Baggage, "priority-bag", 19),

            // Service
            Create("Dedicated Check-in", "Separate check-in counters", AmenityCategory.Service, "checkin", 20),
            Create("Lounge Access", "Access to airline lounges", AmenityCategory.Service, "lounge", 21),
            Create("Concierge Service", "Personal assistance", AmenityCategory.Service, "concierge", 22),
            Create("Fast Track Security", "Expedited security screening", AmenityCategory.Service, "security", 23)
        };
    }
}

public class FareClassAmenity : BaseEntity
{
    public Guid FareClassId { get; set; }
    public Guid AmenityId { get; set; }
    public bool IsIncluded { get; set; } = true;
    public decimal? AdditionalCost { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual FareClass FareClass { get; set; } = null!;
    public virtual Amenity Amenity { get; set; } = null!;

    public static FareClassAmenity Create(
        Guid fareClassId,
        Guid amenityId,
        bool isIncluded = true,
        decimal? additionalCost = null,
        string? notes = null)
    {
        return new FareClassAmenity
        {
            FareClassId = fareClassId,
            AmenityId = amenityId,
            IsIncluded = isIncluded,
            AdditionalCost = additionalCost,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum AmenityCategory
{
    Seating = 0,
    Food_Beverage = 1,
    Entertainment = 2,
    Comfort = 3,
    Baggage = 4,
    Service = 5,
    Other = 6
}

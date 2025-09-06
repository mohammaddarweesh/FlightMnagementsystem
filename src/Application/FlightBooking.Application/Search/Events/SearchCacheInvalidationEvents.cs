using MediatR;

namespace FlightBooking.Application.Search.Events;

public abstract class SearchCacheInvalidationEvent : INotification
{
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class FlightUpdatedEvent : SearchCacheInvalidationEvent
{
    public Guid FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public List<string> UpdatedFields { get; set; } = new();
    public bool AffectsAvailability { get; set; }
    public bool AffectsPricing { get; set; }
}

public class FareClassUpdatedEvent : SearchCacheInvalidationEvent
{
    public Guid FareClassId { get; set; }
    public Guid FlightId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public int? OldCapacity { get; set; }
    public int? NewCapacity { get; set; }
    public bool PriceChanged { get; set; }
    public bool CapacityChanged { get; set; }
    public bool AvailabilityChanged { get; set; }
}

public class BookingConfirmedEvent : SearchCacheInvalidationEvent
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid FlightId { get; set; }
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public List<Guid> SeatIds { get; set; } = new();
    public List<Guid> FareClassIds { get; set; } = new();
    public int PassengerCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class BookingCancelledEvent : SearchCacheInvalidationEvent
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid FlightId { get; set; }
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public List<Guid> ReleasedSeatIds { get; set; } = new();
    public List<Guid> FareClassIds { get; set; } = new();
    public int ReleasedSeatCount { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
}

public class SeatStatusChangedEvent : SearchCacheInvalidationEvent
{
    public List<Guid> SeatIds { get; set; } = new();
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public int AffectedSeatCount { get; set; }
    public bool AvailabilityIncreased { get; set; }
    public bool AvailabilityDecreased { get; set; }
}

public class RouteUpdatedEvent : SearchCacheInvalidationEvent
{
    public Guid RouteId { get; set; }
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public List<string> UpdatedFields { get; set; } = new();
    public bool AffectsFlightSchedules { get; set; }
    public List<Guid> AffectedFlightIds { get; set; } = new();
}

public class AmenityUpdatedEvent : SearchCacheInvalidationEvent
{
    public Guid AmenityId { get; set; }
    public string AmenityName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<Guid> AffectedFlightIds { get; set; } = new();
    public List<Guid> AffectedFareClassIds { get; set; } = new();
    public bool IsActive { get; set; }
}

public class BulkPriceUpdateEvent : SearchCacheInvalidationEvent
{
    public List<Guid> FlightIds { get; set; } = new();
    public List<Guid> FareClassIds { get; set; } = new();
    public string UpdateType { get; set; } = string.Empty; // "seasonal", "demand", "promotional", etc.
    public decimal AverageChangePercentage { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class ScheduleChangedEvent : SearchCacheInvalidationEvent
{
    public List<Guid> FlightIds { get; set; } = new();
    public string ChangeType { get; set; } = string.Empty; // "time", "date", "cancellation", "delay"
    public DateTime? OldDepartureDateTime { get; set; }
    public DateTime? NewDepartureDateTime { get; set; }
    public DateTime? OldArrivalDateTime { get; set; }
    public DateTime? NewArrivalDateTime { get; set; }
    public List<string> AffectedRoutes { get; set; } = new();
    public List<DateTime> AffectedDates { get; set; } = new();
}

public class InventoryAdjustmentEvent : SearchCacheInvalidationEvent
{
    public Guid FlightId { get; set; }
    public Dictionary<Guid, int> FareClassCapacityChanges { get; set; } = new(); // FareClassId -> Capacity Change
    public Dictionary<Guid, int> AvailabilityChanges { get; set; } = new(); // FareClassId -> Availability Change
    public string AdjustmentReason { get; set; } = string.Empty;
    public bool IsTemporary { get; set; }
    public DateTime? RevertAt { get; set; }
}

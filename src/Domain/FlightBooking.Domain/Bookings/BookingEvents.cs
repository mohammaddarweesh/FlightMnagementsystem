using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Bookings;

public class BookingEvent : BaseEntity
{
    public Guid BookingId { get; set; }
    public BookingEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TriggeredBy { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? ExternalReference { get; set; }
    public bool IsSystemGenerated { get; set; }

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;

    public static BookingEvent Create(
        Guid bookingId,
        BookingEventType eventType,
        string description,
        string triggeredBy,
        Dictionary<string, object>? metadata = null,
        string? externalReference = null,
        bool isSystemGenerated = false)
    {
        return new BookingEvent
        {
            BookingId = bookingId,
            EventType = eventType,
            Description = description,
            TriggeredBy = triggeredBy,
            TriggeredAt = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object>(),
            ExternalReference = externalReference,
            IsSystemGenerated = isSystemGenerated,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class BookingModification : BaseEntity
{
    public Guid BookingId { get; set; }
    public BookingModificationType ModificationType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
    public Dictionary<string, object> PreviousValues { get; set; } = new();
    public Dictionary<string, object> NewValues { get; set; } = new();
    public decimal? CostImpact { get; set; }
    public string? Reason { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;

    public static BookingModification Create(
        Guid bookingId,
        BookingModificationType modificationType,
        string description,
        string modifiedBy,
        Dictionary<string, object>? previousValues = null,
        Dictionary<string, object>? newValues = null,
        decimal? costImpact = null,
        string? reason = null,
        bool requiresApproval = false)
    {
        return new BookingModification
        {
            BookingId = bookingId,
            ModificationType = modificationType,
            Description = description,
            ModifiedBy = modifiedBy,
            ModifiedAt = DateTime.UtcNow,
            PreviousValues = previousValues ?? new Dictionary<string, object>(),
            NewValues = newValues ?? new Dictionary<string, object>(),
            CostImpact = costImpact,
            Reason = reason,
            RequiresApproval = requiresApproval,
            IsApproved = !requiresApproval,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(string approvedBy)
    {
        if (!RequiresApproval)
            throw new InvalidOperationException("Modification does not require approval");

        if (IsApproved)
            throw new InvalidOperationException("Modification is already approved");

        IsApproved = true;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class BookingStateTransition
{
    public BookingStatus FromStatus { get; }
    public BookingStatus ToStatus { get; }
    public string Action { get; }
    public Func<Booking, bool> CanTransition { get; }
    public Action<Booking, string>? OnTransition { get; }

    public BookingStateTransition(
        BookingStatus fromStatus,
        BookingStatus toStatus,
        string action,
        Func<Booking, bool> canTransition,
        Action<Booking, string>? onTransition = null)
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Action = action;
        CanTransition = canTransition;
        OnTransition = onTransition;
    }
}

public static class BookingStateMachine
{
    private static readonly List<BookingStateTransition> _transitions = new()
    {
        // Simplified transitions for existing Booking structure
        new(BookingStatus.Draft, BookingStatus.PaymentPending, "submit_for_payment",
            booking => true),

        new(BookingStatus.Draft, BookingStatus.Cancelled, "cancel",
            booking => true,
            (booking, user) => booking.CancelledAt = DateTime.UtcNow),

        new(BookingStatus.PaymentPending, BookingStatus.Confirmed, "confirm_payment",
            booking => true),

        new(BookingStatus.PaymentPending, BookingStatus.Cancelled, "cancel",
            booking => true,
            (booking, user) => booking.CancelledAt = DateTime.UtcNow),

        new(BookingStatus.Confirmed, BookingStatus.CheckedIn, "check_in",
            booking => true),

        new(BookingStatus.Confirmed, BookingStatus.Cancelled, "cancel",
            booking => booking.CanBeCancelled,
            (booking, user) => booking.CancelledAt = DateTime.UtcNow),

        new(BookingStatus.CheckedIn, BookingStatus.Completed, "complete",
            booking => true),
    };

    public static bool CanTransition(Booking booking, BookingStatus toStatus, string action)
    {
        var transition = _transitions.FirstOrDefault(t =>
            t.FromStatus == booking.Status &&
            t.ToStatus == toStatus &&
            t.Action.Equals(action, StringComparison.OrdinalIgnoreCase));

        return transition?.CanTransition(booking) ?? false;
    }

    public static void Transition(Booking booking, BookingStatus toStatus, string action, string user)
    {
        if (!CanTransition(booking, toStatus, action))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {booking.Status} to {toStatus} with action '{action}'");
        }

        var transition = _transitions.First(t =>
            t.FromStatus == booking.Status &&
            t.ToStatus == toStatus &&
            t.Action.Equals(action, StringComparison.OrdinalIgnoreCase));

        var previousStatus = booking.Status;
        booking.Status = toStatus;
        booking.UpdatedAt = DateTime.UtcNow;

        transition.OnTransition?.Invoke(booking, user);

        // Add event for state transition
        var eventDescription = $"Status changed from {previousStatus} to {toStatus}";
        var metadata = new Dictionary<string, object>
        {
            ["previous_status"] = previousStatus.ToString(),
            ["new_status"] = toStatus.ToString(),
            ["action"] = action,
            ["user"] = user
        };

        // This would typically be handled by domain events
        // For now, we'll add it directly to the booking
    }

    public static List<string> GetAvailableActions(Booking booking)
    {
        return _transitions
            .Where(t => t.FromStatus == booking.Status && t.CanTransition(booking))
            .Select(t => t.Action)
            .ToList();
    }

    public static List<BookingStatus> GetPossibleStatuses(Booking booking)
    {
        return _transitions
            .Where(t => t.FromStatus == booking.Status && t.CanTransition(booking))
            .Select(t => t.ToStatus)
            .ToList();
    }
}



public enum BookingEventType
{
    Created,
    Updated,
    Confirmed,
    Cancelled,
    CheckedIn,
    Boarded,
    Completed,
    PaymentReceived,
    PaymentFailed,
    RefundProcessed,
    SeatAssigned,
    SeatChanged,
    PassengerAdded,
    PassengerUpdated,
    ExtraAdded,
    ExtraRemoved,
    ContactUpdated,
    DatesChanged,
    FareClassUpgraded,
    PromoCodeApplied,
    ModificationRequested,
    ModificationApproved,
    ModificationRejected,
    EmailSent,
    SMSSent,
    DocumentsUploaded,
    SpecialRequestAdded,
    NoShow,
    Expired,
    Reactivated
}

public enum BookingModificationType
{
    PassengerAdded,
    PassengerUpdated,
    PassengerRemoved,
    SeatAssigned,
    SeatChanged,
    SeatRemoved,
    ExtraAdded,
    ExtraRemoved,
    ExtraUpdated,
    ContactUpdated,
    DatesChanged,
    FareClassUpgraded,
    FareClassDowngraded,
    PromoCodeApplied,
    PromoCodeRemoved,
    SpecialRequestAdded,
    SpecialRequestUpdated,
    PaymentMethodChanged,
    BillingAddressUpdated,
    EmergencyContactUpdated,
    DocumentsUpdated,
    PreferencesUpdated
}

public enum BookingType
{
    OneWay,
    RoundTrip,
    MultiCity,
    OpenJaw
}

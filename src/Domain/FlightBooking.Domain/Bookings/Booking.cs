using FlightBooking.Domain.Common;
using FlightBooking.Domain.Flights;
using FlightBooking.Domain.Identity;

namespace FlightBooking.Domain.Bookings;

public class Booking : BaseEntity
{
    public string BookingReference { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid FlightId { get; set; }
    public Guid? UserId { get; set; }
    public string? GuestId { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? PaymentStatus { get; set; }
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
    public bool IsArchived { get; set; } = false;

    // Navigation properties
    public virtual Flight Flight { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
    public virtual ICollection<BookingPassenger> BookingPassengers { get; set; } = new List<BookingPassenger>();
    public virtual ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();

    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => Status == BookingStatus.Confirmed && !IsExpired;
    public bool CanBeCancelled => Status == BookingStatus.Confirmed && !IsExpired;
    public bool RequiresPayment => Status == BookingStatus.PaymentPending && !IsExpired;
    public int PassengerCount => BookingPassengers.Count;
    public int SeatCount => BookingSeats.Count;

    // Helper methods
    public string GetDisplayReference() => $"FB{BookingReference}";

    public void Confirm(string paymentIntentId)
    {
        if (Status != BookingStatus.PaymentPending)
            throw new InvalidOperationException($"Cannot confirm booking in status {Status}");

        if (IsExpired)
            throw new InvalidOperationException("Cannot confirm expired booking");

        Status = BookingStatus.Confirmed;
        PaymentIntentId = paymentIntentId;
        PaymentStatus = "succeeded";
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (!CanBeCancelled && Status != BookingStatus.PaymentPending)
            throw new InvalidOperationException($"Cannot cancel booking in status {Status}");

        Status = BookingStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status != BookingStatus.PaymentPending)
            return;

        Status = BookingStatus.Expired;
        CancellationReason = "Booking expired";
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkEmailSent()
    {
        EmailSent = true;
        EmailSentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordError(string error)
    {
        LastError = error;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Booking Create(
        string idempotencyKey,
        Guid flightId,
        Guid? userId,
        string? guestId,
        string contactEmail,
        string contactPhone,
        decimal totalAmount,
        TimeSpan holdDuration)
    {
        var bookingReference = GenerateBookingReference();
        
        return new Booking
        {
            BookingReference = bookingReference,
            IdempotencyKey = idempotencyKey,
            FlightId = flightId,
            UserId = userId,
            GuestId = guestId,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            TotalAmount = totalAmount,
            Status = BookingStatus.PaymentPending,
            ExpiresAt = DateTime.UtcNow.Add(holdDuration),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string GenerateBookingReference()
    {
        // Generate a unique 8-character alphanumeric reference
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

public class BookingSeat : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid SeatId { get; set; }
    public Guid PassengerId { get; set; }
    public decimal SeatPrice { get; set; }
    public decimal? ExtraFee { get; set; }
    public SeatHoldStatus HoldStatus { get; set; } = SeatHoldStatus.Held;
    public DateTime HeldAt { get; set; }
    public DateTime? ReleasedAt { get; set; }

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;
    public virtual Seat Seat { get; set; } = null!;
    public virtual BookingPassenger Passenger { get; set; } = null!;

    // Computed properties
    public decimal TotalPrice => SeatPrice + (ExtraFee ?? 0);
    public bool IsHeld => HoldStatus == SeatHoldStatus.Held;
    public bool IsReleased => HoldStatus == SeatHoldStatus.Released;

    public void Release()
    {
        HoldStatus = SeatHoldStatus.Released;
        ReleasedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static BookingSeat Create(
        Guid bookingId,
        Guid seatId,
        Guid passengerId,
        decimal seatPrice,
        decimal? extraFee = null)
    {
        return new BookingSeat
        {
            BookingId = bookingId,
            SeatId = seatId,
            PassengerId = passengerId,
            SeatPrice = seatPrice,
            ExtraFee = extraFee,
            HoldStatus = SeatHoldStatus.Held,
            HeldAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class BookingPassenger : BaseEntity
{
    public Guid BookingId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string PassportCountry { get; set; } = string.Empty;
    public DateTime PassportExpiry { get; set; }
    public string? SpecialRequests { get; set; }
    public bool IsInfant { get; set; } = false;

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;
    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public int Age => DateTime.Today.Year - DateOfBirth.Year - 
                     (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

    public static BookingPassenger Create(
        Guid bookingId,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        string gender,
        string passportNumber,
        string passportCountry,
        DateTime passportExpiry)
    {
        return new BookingPassenger
        {
            BookingId = bookingId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            PassportNumber = passportNumber,
            PassportCountry = passportCountry,
            PassportExpiry = passportExpiry,
            IsInfant = DateTime.Today.Year - dateOfBirth.Year < 2,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum BookingStatus
{
    Draft = 0,
    Pending = 1,
    PaymentPending = 2,
    Confirmed = 3,
    CheckedIn = 4,
    Boarded = 5,
    Completed = 6,
    Cancelled = 7,
    Expired = 8,
    PaymentFailed = 9,
    NoShow = 10,
    Refunded = 11,
    PartiallyRefunded = 12
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

public class BookingItem : BaseEntity
{
    public Guid BookingId { get; set; }
    public string ItemType { get; set; } = string.Empty; // "Flight", "Seat", "Baggage", "Meal", etc.
    public string ItemName { get; set; } = string.Empty;
    public string? ItemDescription { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice { get; set; }
    public string? ItemData { get; set; } // JSON data for item-specific information

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;

    public static BookingItem Create(
        Guid bookingId,
        string itemType,
        string itemName,
        decimal unitPrice,
        int quantity = 1,
        string? description = null,
        string? itemData = null)
    {
        return new BookingItem
        {
            BookingId = bookingId,
            ItemType = itemType,
            ItemName = itemName,
            ItemDescription = description,
            UnitPrice = unitPrice,
            Quantity = quantity,
            TotalPrice = unitPrice * quantity,
            ItemData = itemData,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum SeatHoldStatus
{
    Held = 0,
    Released = 1,
    Confirmed = 2
}

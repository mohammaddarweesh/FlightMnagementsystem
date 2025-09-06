using FlightBooking.Domain.Common;
using FlightBooking.Domain.Flights;

namespace FlightBooking.Domain.Bookings;

public class BookingRequest : BaseEntity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public BookingRequestStatus Status { get; set; } = BookingRequestStatus.Processing;
    public string RequestData { get; set; } = string.Empty; // JSON serialized request
    public string? ResponseData { get; set; } // JSON serialized response
    public string? ErrorMessage { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ProcessingAttempts { get; set; } = 0;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public virtual Booking? Booking { get; set; }

    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsCompleted => Status == BookingRequestStatus.Completed;
    public bool IsFailed => Status == BookingRequestStatus.Failed;
    public bool CanRetry => Status == BookingRequestStatus.Failed && ProcessingAttempts < 3 && !IsExpired;

    public void MarkCompleted(Guid bookingId, string responseData)
    {
        BookingId = bookingId;
        Status = BookingRequestStatus.Completed;
        ResponseData = responseData;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = BookingRequestStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessingAttempts++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRetrying()
    {
        Status = BookingRequestStatus.Processing;
        ProcessingAttempts++;
        UpdatedAt = DateTime.UtcNow;
    }

    public static BookingRequest Create(
        string idempotencyKey,
        string requestHash,
        string requestData,
        TimeSpan expiration)
    {
        return new BookingRequest
        {
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            RequestData = requestData,
            Status = BookingRequestStatus.Processing,
            ExpiresAt = DateTime.UtcNow.Add(expiration),
            ProcessingAttempts = 1,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class SeatHold : BaseEntity
{
    public Guid SeatId { get; set; }
    public Guid? BookingId { get; set; }
    public string HoldReference { get; set; } = string.Empty;
    public string? UserId { get; set; } // Can be user ID or guest ID
    public SeatHoldStatus Status { get; set; } = SeatHoldStatus.Held;
    public DateTime HeldAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleaseReason { get; set; }

    // Navigation properties
    public virtual Seat Seat { get; set; } = null!;
    public virtual Booking? Booking { get; set; }

    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => Status == SeatHoldStatus.Held && !IsExpired;
    public TimeSpan RemainingTime => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.UtcNow;

    public void Release(string reason)
    {
        Status = SeatHoldStatus.Released;
        ReleaseReason = reason;
        ReleasedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm(Guid bookingId)
    {
        if (Status != SeatHoldStatus.Held)
            throw new InvalidOperationException($"Cannot confirm seat hold in status {Status}");

        if (IsExpired)
            throw new InvalidOperationException("Cannot confirm expired seat hold");

        BookingId = bookingId;
        Status = SeatHoldStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status == SeatHoldStatus.Held)
        {
            Release("Hold expired");
        }
    }

    public static SeatHold Create(
        Guid seatId,
        string userId,
        TimeSpan holdDuration)
    {
        var holdReference = GenerateHoldReference();
        
        return new SeatHold
        {
            SeatId = seatId,
            UserId = userId,
            HoldReference = holdReference,
            Status = SeatHoldStatus.Held,
            HeldAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(holdDuration),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string GenerateHoldReference()
    {
        return $"HOLD_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..20];
    }
}

public enum BookingRequestStatus
{
    Processing = 0,
    Completed = 1,
    Failed = 2
}

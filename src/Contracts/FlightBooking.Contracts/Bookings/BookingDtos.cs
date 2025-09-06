using FlightBooking.Contracts.Common;

namespace FlightBooking.Contracts.Bookings;

public class CreateBookingRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid FlightId { get; set; }
    public Guid? UserId { get; set; }
    public string? GuestId { get; set; }
    public List<Guid> SeatIds { get; set; } = new();
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public List<PassengerRequest> Passengers { get; set; } = new();
}

public class PassengerRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string PassportCountry { get; set; } = string.Empty;
    public DateTime PassportExpiry { get; set; }
    public string? SpecialRequests { get; set; }
}

public class SeatHoldRequest
{
    public Guid FlightId { get; set; }
    public List<Guid> SeatIds { get; set; } = new();
    public string UserId { get; set; } = string.Empty; // Can be user ID or guest ID
}

public class ConfirmBookingRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<string> HoldReferences { get; set; } = new();
    public Guid? UserId { get; set; }
    public string? GuestId { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public List<PassengerRequest> Passengers { get; set; } = new();
}

public class ReleaseSeatRequest
{
    public List<Guid>? SeatIds { get; set; }
    public List<string>? HoldReferences { get; set; }
    public Guid? BookingId { get; set; }
    public string? Reason { get; set; }
}

public class BookingResult : BaseResponse
{
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class SeatHoldResult : BaseResponse
{
    public List<string> HoldReferences { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public List<HeldSeatDto> HeldSeats { get; set; } = new();
    public List<Guid> UnavailableSeats { get; set; } = new();
}

public class HeldSeatDto
{
    public Guid SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class AvailableSeatsResult : BaseResponse
{
    public List<AvailableSeatDto> AvailableSeats { get; set; } = new();
    public int TotalCount { get; set; }
}

public class AvailableSeatDto
{
    public Guid SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string FareClassName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal? ExtraFee { get; set; }
    public decimal TotalPrice { get; set; }
    public string SeatType { get; set; } = string.Empty;
    public bool IsWindow { get; set; }
    public bool IsAisle { get; set; }
}

public class BookingDto
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string DisplayReference { get; set; } = string.Empty;
    public Guid FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public List<BookingPassengerDto> Passengers { get; set; } = new();
    public List<BookingSeatDto> Seats { get; set; } = new();
}

public class BookingPassengerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string PassportCountry { get; set; } = string.Empty;
    public DateTime PassportExpiry { get; set; }
    public bool IsInfant { get; set; }
    public string? SpecialRequests { get; set; }
}

public class BookingSeatDto
{
    public Guid Id { get; set; }
    public Guid SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string PassengerName { get; set; } = string.Empty;
    public decimal SeatPrice { get; set; }
    public decimal? ExtraFee { get; set; }
    public decimal TotalPrice { get; set; }
    public string HoldStatus { get; set; } = string.Empty;
    public DateTime HeldAt { get; set; }
}

public class GetBookingsResponse : BaseResponse
{
    public List<BookingDto> Bookings { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class GetBookingResponse : BaseResponse
{
    public BookingDto? Booking { get; set; }
}

using FlightBooking.Contracts.Bookings;
using FlightBooking.Contracts.Common;

namespace FlightBooking.Application.Bookings.Services;

public interface ISeatInventoryService
{
    /// <summary>
    /// Attempts to hold seats for a booking with strong consistency guarantees
    /// </summary>
    Task<SeatHoldResult> HoldSeatsAsync(
        SeatHoldRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms held seats and creates a booking
    /// </summary>
    Task<BookingResult> ConfirmBookingAsync(
        ConfirmBookingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases held seats (compensation logic)
    /// </summary>
    Task<BaseResponse> ReleaseSeatsAsync(
        ReleaseSeatRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a complete booking with idempotency
    /// </summary>
    Task<BookingResult> ProcessBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a booking and releases seats
    /// </summary>
    Task<BaseResponse> CancelBookingAsync(
        Guid bookingId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires old seat holds and bookings
    /// </summary>
    Task<BaseResponse> ExpireOldHoldsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available seats for a flight with real-time status
    /// </summary>
    Task<AvailableSeatsResult> GetAvailableSeatsAsync(
        Guid flightId,
        Guid? fareClassId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed booking operations
    /// </summary>
    Task<BookingResult> RetryBookingAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

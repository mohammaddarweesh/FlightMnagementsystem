using FlightBooking.Domain.Flights;

namespace FlightBooking.Infrastructure.Bookings.Repositories;

public interface ISeatInventoryRepository
{
    /// <summary>
    /// Gets available seats with PostgreSQL row-level locks (FOR UPDATE SKIP LOCKED)
    /// </summary>
    Task<List<Seat>> GetAvailableSeatsWithLockAsync(
        Guid flightId,
        List<Guid> seatIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available seats for a flight without locks (read-only)
    /// </summary>
    Task<List<Seat>> GetAvailableSeatsAsync(
        Guid flightId,
        Guid? fareClassId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to reserve seats atomically with row-level locking
    /// </summary>
    Task<bool> TryReserveSeatsAsync(
        List<Guid> seatIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases reserved seats back to available status
    /// </summary>
    Task<int> ReleaseSeatsAsync(
        List<Guid> seatIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets seat availability counts by fare class
    /// </summary>
    Task<Dictionary<Guid, int>> GetSeatAvailabilityByFareClassAsync(
        Guid flightId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if specific seats are still available
    /// </summary>
    Task<List<Guid>> CheckSeatAvailabilityAsync(
        List<Guid> seatIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets seats with their current hold status
    /// </summary>
    Task<List<SeatWithHoldInfo>> GetSeatsWithHoldInfoAsync(
        Guid flightId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update seat statuses with optimistic concurrency
    /// </summary>
    Task<bool> BulkUpdateSeatStatusAsync(
        Dictionary<Guid, SeatStatus> seatStatusUpdates,
        CancellationToken cancellationToken = default);
}

public class SeatWithHoldInfo
{
    public Guid SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public SeatStatus Status { get; set; }
    public bool IsHeld { get; set; }
    public DateTime? HoldExpiresAt { get; set; }
    public string? HoldReference { get; set; }
    public decimal Price { get; set; }
    public decimal? ExtraFee { get; set; }
}

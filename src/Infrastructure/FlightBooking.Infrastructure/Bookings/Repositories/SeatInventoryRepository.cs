using FlightBooking.Domain.Bookings;
using FlightBooking.Domain.Flights;
using FlightBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Bookings.Repositories;

public class SeatInventoryRepository : ISeatInventoryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SeatInventoryRepository> _logger;

    public SeatInventoryRepository(ApplicationDbContext context, ILogger<SeatInventoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Seat>> GetAvailableSeatsWithLockAsync(
        Guid flightId,
        List<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use raw SQL with FOR UPDATE SKIP LOCKED for strong consistency
            var sql = @"
                SELECT s.""Id"", s.""FlightId"", s.""FareClassId"", s.""SeatNumber"", s.""Row"", s.""Column"",
                       s.""Type"", s.""Status"", s.""ExtraFee"", s.""IsActive"", s.""Notes"",
                       s.""CreatedAt"", s.""UpdatedAt""
                FROM ""Seats"" s
                WHERE s.""FlightId"" = @flightId 
                  AND s.""Id"" = ANY(@seatIds)
                  AND s.""Status"" = @availableStatus
                  AND s.""IsActive"" = true
                FOR UPDATE SKIP LOCKED";

            var parameters = new object[]
            {
                flightId,
                seatIds.ToArray(),
                (int)SeatStatus.Available
            };

            var seats = await _context.Seats
                .FromSqlRaw(sql, parameters)
                .Include(s => s.FareClass)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Locked {Count} seats out of {Requested} requested for flight {FlightId}", 
                seats.Count, seatIds.Count, flightId);

            return seats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available seats with lock for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<List<Seat>> GetAvailableSeatsAsync(
        Guid flightId,
        Guid? fareClassId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Seats
                .Include(s => s.FareClass)
                .Where(s => s.FlightId == flightId && 
                           s.Status == SeatStatus.Available && 
                           s.IsActive);

            if (fareClassId.HasValue)
            {
                query = query.Where(s => s.FareClassId == fareClassId.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available seats for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<bool> TryReserveSeatsAsync(
        List<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use raw SQL for atomic update with row-level locking
            var sql = @"
                UPDATE ""Seats""
                SET ""Status"" = @reservedStatus, ""UpdatedAt"" = @updatedAt
                WHERE ""Id"" = ANY(@seatIds)
                  AND ""Status"" = @availableStatus
                  AND ""IsActive"" = true";

            var parameters = new object[]
            {
                seatIds.ToArray(),
                (int)SeatStatus.Reserved,
                (int)SeatStatus.Available,
                DateTime.UtcNow
            };

            var updatedCount = await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
            
            var success = updatedCount == seatIds.Count;
            
            _logger.LogDebug("Reserved {UpdatedCount} out of {RequestedCount} seats. Success: {Success}", 
                updatedCount, seatIds.Count, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving seats {SeatIds}", string.Join(",", seatIds));
            throw;
        }
    }

    public async Task<int> ReleaseSeatsAsync(
        List<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = @"
                UPDATE ""Seats""
                SET ""Status"" = @availableStatus, ""UpdatedAt"" = @updatedAt
                WHERE ""Id"" = ANY(@seatIds)
                  AND ""Status"" IN (@reservedStatus, @occupiedStatus)
                  AND ""IsActive"" = true";

            var parameters = new object[]
            {
                seatIds.ToArray(),
                (int)SeatStatus.Available,
                (int)SeatStatus.Reserved,
                (int)SeatStatus.Occupied,
                DateTime.UtcNow
            };

            var releasedCount = await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
            
            _logger.LogDebug("Released {ReleasedCount} seats", releasedCount);
            
            return releasedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing seats {SeatIds}", string.Join(",", seatIds));
            throw;
        }
    }

    public async Task<Dictionary<Guid, int>> GetSeatAvailabilityByFareClassAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var availability = await _context.Seats
                .Where(s => s.FlightId == flightId && 
                           s.Status == SeatStatus.Available && 
                           s.IsActive)
                .GroupBy(s => s.FareClassId)
                .Select(g => new { FareClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FareClassId, x => x.Count, cancellationToken);

            return availability;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat availability for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<List<Guid>> CheckSeatAvailabilityAsync(
        List<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var availableSeats = await _context.Seats
                .Where(s => seatIds.Contains(s.Id) && 
                           s.Status == SeatStatus.Available && 
                           s.IsActive)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            return availableSeats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking seat availability for seats {SeatIds}", string.Join(",", seatIds));
            throw;
        }
    }

    public async Task<List<SeatWithHoldInfo>> GetSeatsWithHoldInfoAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var seatsWithHolds = await _context.Seats
                .Include(s => s.FareClass)
                .Where(s => s.FlightId == flightId && s.IsActive)
                .GroupJoin(
                    _context.SeatHolds.Where(sh => sh.Status == SeatHoldStatus.Held),
                    seat => seat.Id,
                    hold => hold.SeatId,
                    (seat, holds) => new { Seat = seat, Holds = holds })
                .SelectMany(
                    x => x.Holds.DefaultIfEmpty(),
                    (x, hold) => new SeatWithHoldInfo
                    {
                        SeatId = x.Seat.Id,
                        SeatNumber = x.Seat.SeatNumber,
                        Status = x.Seat.Status,
                        IsHeld = hold != null && hold.Status == SeatHoldStatus.Held && !hold.IsExpired,
                        HoldExpiresAt = hold != null ? hold.ExpiresAt : null,
                        HoldReference = hold != null ? hold.HoldReference : null,
                        Price = x.Seat.FareClass.CurrentPrice,
                        ExtraFee = x.Seat.ExtraFee
                    })
                .ToListAsync(cancellationToken);

            return seatsWithHolds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seats with hold info for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<bool> BulkUpdateSeatStatusAsync(
        Dictionary<Guid, SeatStatus> seatStatusUpdates,
        CancellationToken cancellationToken = default)
    {
        if (!seatStatusUpdates.Any())
            return true;

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var seatIds = seatStatusUpdates.Keys.ToList();
            var seats = await _context.Seats
                .Where(s => seatIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var updatedCount = 0;
            foreach (var seat in seats)
            {
                if (seatStatusUpdates.TryGetValue(seat.Id, out var newStatus))
                {
                    seat.Status = newStatus;
                    seat.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var success = updatedCount == seatStatusUpdates.Count;

            _logger.LogDebug("Bulk updated {UpdatedCount} out of {RequestedCount} seat statuses. Success: {Success}",
                updatedCount, seatStatusUpdates.Count, success);

            return success;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error bulk updating seat statuses");
            throw;
        }
    }
}

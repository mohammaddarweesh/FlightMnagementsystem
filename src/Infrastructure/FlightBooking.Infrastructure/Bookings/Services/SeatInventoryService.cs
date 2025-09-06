using FlightBooking.Application.Bookings.Services;
using FlightBooking.Contracts.Bookings;
using FlightBooking.Contracts.Common;
using FlightBooking.Domain.Bookings;
using FlightBooking.Domain.Flights;
using FlightBooking.Infrastructure.Bookings.Repositories;
using FlightBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedLockNet;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FlightBooking.Infrastructure.Bookings.Services;

public class SeatInventoryService : ISeatInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ISeatInventoryRepository _seatRepository;
    // private readonly IRedLockFactory _redLockFactory;
    private readonly ILogger<SeatInventoryService> _logger;
    private readonly TimeSpan _defaultHoldDuration = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(30);

    public SeatInventoryService(
        ApplicationDbContext context,
        ISeatInventoryRepository seatRepository,
        // IRedLockFactory redLockFactory,
        ILogger<SeatInventoryService> logger)
    {
        _context = context;
        _seatRepository = seatRepository;
        // _redLockFactory = redLockFactory;
        _logger = logger;
    }

    public async Task<BookingResult> ProcessBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestHash = ComputeRequestHash(request);
        var lockKey = $"booking:process:{request.IdempotencyKey}";

        // Check for existing request first (fast path)
        var existingRequest = await _context.BookingRequests
            .Include(br => br.Booking)
            .FirstOrDefaultAsync(br => br.IdempotencyKey == request.IdempotencyKey, cancellationToken);

        if (existingRequest != null)
        {
            return await HandleExistingRequest(existingRequest, requestHash);
        }

        // Use database-level locking for the critical section
        // TODO: Implement Redis distributed lock when RedLock.net is properly configured
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Double-check for existing request after acquiring lock
            existingRequest = await _context.BookingRequests
                .Include(br => br.Booking)
                .FirstOrDefaultAsync(br => br.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (existingRequest != null)
            {
                return await HandleExistingRequest(existingRequest, requestHash);
            }

            // Create new booking request
            var bookingRequest = BookingRequest.Create(
                request.IdempotencyKey,
                requestHash,
                JsonSerializer.Serialize(request),
                TimeSpan.FromHours(1));

            _context.BookingRequests.Add(bookingRequest);
            await _context.SaveChangesAsync(cancellationToken);

            // Process the booking
            return await ProcessNewBooking(request, bookingRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing booking {IdempotencyKey}", request.IdempotencyKey);
            return new BookingResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing your booking"
            };
        }
    }

    public async Task<SeatHoldResult> HoldSeatsAsync(
        SeatHoldRequest request,
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"seats:hold:{request.FlightId}";
        
        // TODO: Implement Redis distributed lock
        // await using var redLock = await _redLockFactory.CreateLockAsync(lockKey, _lockTimeout);

        // For now, use database transactions for concurrency control
        // TODO: Add Redis distributed lock validation when implemented

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Get available seats with row-level locks
            var availableSeats = await _seatRepository.GetAvailableSeatsWithLockAsync(
                request.FlightId,
                request.SeatIds,
                cancellationToken);

            if (availableSeats.Count != request.SeatIds.Count)
            {
                var unavailableSeats = request.SeatIds.Except(availableSeats.Select(s => s.Id)).ToList();
                return new SeatHoldResult
                {
                    Success = false,
                    ErrorMessage = "Some selected seats are no longer available",
                    UnavailableSeats = unavailableSeats
                };
            }

            // Create seat holds
            var seatHolds = new List<SeatHold>();
            foreach (var seat in availableSeats)
            {
                var hold = SeatHold.Create(seat.Id, request.UserId, _defaultHoldDuration);
                seatHolds.Add(hold);
                _context.SeatHolds.Add(hold);

                // Update seat status to reserved
                seat.Status = SeatStatus.Reserved;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully held {Count} seats for user {UserId}", 
                seatHolds.Count, request.UserId);

            return new SeatHoldResult
            {
                Success = true,
                HoldReferences = seatHolds.Select(h => h.HoldReference).ToList(),
                ExpiresAt = seatHolds.First().ExpiresAt,
                HeldSeats = availableSeats.Select(s => new HeldSeatDto
                {
                    SeatId = s.Id,
                    SeatNumber = s.SeatNumber,
                    Price = s.FareClass.CurrentPrice + (s.ExtraFee ?? 0)
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error holding seats for flight {FlightId}", request.FlightId);
            
            return new SeatHoldResult
            {
                Success = false,
                ErrorMessage = "Unable to hold seats, please try again"
            };
        }
    }

    public async Task<BookingResult> ConfirmBookingAsync(
        ConfirmBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"booking:confirm:{string.Join(",", request.HoldReferences)}";
        
        // TODO: Implement Redis distributed lock
        // Use database transactions for now

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Get and validate seat holds
            var seatHolds = await _context.SeatHolds
                .Include(sh => sh.Seat)
                    .ThenInclude(s => s.FareClass)
                .Where(sh => request.HoldReferences.Contains(sh.HoldReference) && 
                           sh.Status == SeatHoldStatus.Held)
                .ToListAsync(cancellationToken);

            if (seatHolds.Count != request.HoldReferences.Count)
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "Some seat holds have expired or are invalid"
                };
            }

            // Check if any holds have expired
            var expiredHolds = seatHolds.Where(sh => sh.IsExpired).ToList();
            if (expiredHolds.Any())
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "Some seat holds have expired"
                };
            }

            // Calculate total amount
            var totalAmount = seatHolds.Sum(sh => sh.Seat.FareClass.CurrentPrice + (sh.Seat.ExtraFee ?? 0));

            // Create booking
            var booking = Booking.Create(
                request.IdempotencyKey,
                seatHolds.First().Seat.FlightId,
                request.UserId,
                request.GuestId,
                request.ContactEmail,
                request.ContactPhone,
                totalAmount,
                TimeSpan.FromHours(24));

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(cancellationToken);

            // Create passengers and booking seats
            foreach (var passenger in request.Passengers)
            {
                var bookingPassenger = BookingPassenger.Create(
                    booking.Id,
                    passenger.FirstName,
                    passenger.LastName,
                    passenger.DateOfBirth,
                    passenger.Gender,
                    passenger.PassportNumber,
                    passenger.PassportCountry,
                    passenger.PassportExpiry);

                _context.BookingPassengers.Add(bookingPassenger);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Link seats to booking
            var passengers = await _context.BookingPassengers
                .Where(bp => bp.BookingId == booking.Id)
                .ToListAsync(cancellationToken);

            for (int i = 0; i < seatHolds.Count && i < passengers.Count; i++)
            {
                var seatHold = seatHolds[i];
                var passenger = passengers[i];

                var bookingSeat = BookingSeat.Create(
                    booking.Id,
                    seatHold.SeatId,
                    passenger.Id,
                    seatHold.Seat.FareClass.CurrentPrice,
                    seatHold.Seat.ExtraFee);

                _context.BookingSeats.Add(bookingSeat);

                // Confirm the seat hold
                seatHold.Confirm(booking.Id);
                
                // Update seat status
                seatHold.Seat.Status = SeatStatus.Occupied;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully created booking {BookingReference} for {SeatCount} seats", 
                booking.BookingReference, seatHolds.Count);

            return new BookingResult
            {
                Success = true,
                BookingId = booking.Id,
                BookingReference = booking.GetDisplayReference(),
                TotalAmount = booking.TotalAmount,
                ExpiresAt = booking.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error confirming booking for holds {HoldReferences}", 
                string.Join(",", request.HoldReferences));
            
            return new BookingResult
            {
                Success = false,
                ErrorMessage = "Unable to confirm booking, please try again"
            };
        }
    }

    public async Task<BaseResponse> ReleaseSeatsAsync(
        ReleaseSeatRequest request,
        CancellationToken cancellationToken = default)
    {
        var identifiers = new List<string>();
        if (request.SeatIds?.Any() == true)
            identifiers.AddRange(request.SeatIds.Select(id => id.ToString()));
        if (request.HoldReferences?.Any() == true)
            identifiers.AddRange(request.HoldReferences);

        var lockKey = $"seats:release:{string.Join(",", identifiers)}";

        // TODO: Implement Redis distributed lock
        // Use database transactions for now

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var releasedCount = 0;

            // Release by hold references
            if (request.HoldReferences?.Any() == true)
            {
                var seatHolds = await _context.SeatHolds
                    .Include(sh => sh.Seat)
                    .Where(sh => request.HoldReferences.Contains(sh.HoldReference) &&
                               sh.Status == SeatHoldStatus.Held)
                    .ToListAsync(cancellationToken);

                foreach (var hold in seatHolds)
                {
                    hold.Release(request.Reason ?? "Manual release");
                    hold.Seat.Status = SeatStatus.Available;
                    releasedCount++;
                }
            }

            // Release by seat IDs
            if (request.SeatIds?.Any() == true)
            {
                var seats = await _context.Seats
                    .Where(s => request.SeatIds.Contains(s.Id) &&
                              s.Status == SeatStatus.Reserved)
                    .ToListAsync(cancellationToken);

                foreach (var seat in seats)
                {
                    seat.Status = SeatStatus.Available;
                    releasedCount++;
                }

                // Also release any active holds for these seats
                var activeHolds = await _context.SeatHolds
                    .Where(sh => request.SeatIds.Contains(sh.SeatId) &&
                               sh.Status == SeatHoldStatus.Held)
                    .ToListAsync(cancellationToken);

                foreach (var hold in activeHolds)
                {
                    hold.Release(request.Reason ?? "Seat release");
                }
            }

            // Release by booking ID
            if (request.BookingId.HasValue)
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingSeats)
                        .ThenInclude(bs => bs.Seat)
                    .FirstOrDefaultAsync(b => b.Id == request.BookingId.Value, cancellationToken);

                if (booking != null)
                {
                    foreach (var bookingSeat in booking.BookingSeats)
                    {
                        bookingSeat.Release();
                        bookingSeat.Seat.Status = SeatStatus.Available;
                        releasedCount++;
                    }

                    // Cancel the booking
                    booking.Cancel(request.Reason ?? "Seat release");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully released {Count} seats. Reason: {Reason}",
                releasedCount, request.Reason);

            return new BaseResponse
            {
                Success = true,
                Message = $"Successfully released {releasedCount} seats"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error releasing seats");

            return new BaseResponse
            {
                Success = false,
                ErrorMessage = "Unable to release seats"
            };
        }
    }

    public async Task<BaseResponse> CancelBookingAsync(
        Guid bookingId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"booking:cancel:{bookingId}";

        // TODO: Implement Redis distributed lock
        // Use database transactions for now

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                return new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Booking not found"
                };
            }

            if (!booking.CanBeCancelled && booking.Status != BookingStatus.PaymentPending)
            {
                return new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Booking cannot be cancelled"
                };
            }

            // Release all seats
            foreach (var bookingSeat in booking.BookingSeats)
            {
                bookingSeat.Release();
                bookingSeat.Seat.Status = SeatStatus.Available;
            }

            // Cancel the booking
            booking.Cancel(reason);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully cancelled booking {BookingReference}. Reason: {Reason}",
                booking.BookingReference, reason);

            return new BaseResponse
            {
                Success = true,
                Message = "Booking cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);

            return new BaseResponse
            {
                Success = false,
                ErrorMessage = "Unable to cancel booking"
            };
        }
    }

    public async Task<BaseResponse> ExpireOldHoldsAsync(CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTime.UtcNow;

            // Expire old seat holds
            var expiredHolds = await _context.SeatHolds
                .Include(sh => sh.Seat)
                .Where(sh => sh.Status == SeatHoldStatus.Held && sh.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            foreach (var hold in expiredHolds)
            {
                hold.Expire();
                hold.Seat.Status = SeatStatus.Available;
            }

            // Expire old bookings
            var expiredBookings = await _context.Bookings
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .Where(b => b.Status == BookingStatus.PaymentPending && b.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            foreach (var booking in expiredBookings)
            {
                booking.Expire();

                foreach (var bookingSeat in booking.BookingSeats)
                {
                    bookingSeat.Release();
                    bookingSeat.Seat.Status = SeatStatus.Available;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Expired {HoldCount} seat holds and {BookingCount} bookings",
                expiredHolds.Count, expiredBookings.Count);

            return new BaseResponse
            {
                Success = true,
                Message = $"Expired {expiredHolds.Count} holds and {expiredBookings.Count} bookings"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error expiring old holds and bookings");

            return new BaseResponse
            {
                Success = false,
                ErrorMessage = "Unable to expire old holds"
            };
        }
    }

    public async Task<AvailableSeatsResult> GetAvailableSeatsAsync(
        Guid flightId,
        Guid? fareClassId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Seats
                .Include(s => s.FareClass)
                .Where(s => s.FlightId == flightId && s.IsActive && s.Status == SeatStatus.Available);

            if (fareClassId.HasValue)
            {
                query = query.Where(s => s.FareClassId == fareClassId.Value);
            }

            var availableSeats = await query
                .Select(s => new AvailableSeatDto
                {
                    SeatId = s.Id,
                    SeatNumber = s.SeatNumber,
                    FareClassName = s.FareClass.ClassName,
                    BasePrice = s.FareClass.CurrentPrice,
                    ExtraFee = s.ExtraFee,
                    TotalPrice = s.FareClass.CurrentPrice + (s.ExtraFee ?? 0),
                    SeatType = s.Type.ToString(),
                    IsWindow = s.IsWindow,
                    IsAisle = s.IsAisle
                })
                .ToListAsync(cancellationToken);

            return new AvailableSeatsResult
            {
                Success = true,
                AvailableSeats = availableSeats,
                TotalCount = availableSeats.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available seats for flight {FlightId}", flightId);
            return new AvailableSeatsResult
            {
                Success = false,
                ErrorMessage = "Unable to retrieve available seats"
            };
        }
    }

    public async Task<BookingResult> RetryBookingAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var bookingRequest = await _context.BookingRequests
            .Include(br => br.Booking)
            .FirstOrDefaultAsync(br => br.IdempotencyKey == idempotencyKey, cancellationToken);

        if (bookingRequest == null)
        {
            return new BookingResult
            {
                Success = false,
                ErrorMessage = "Booking request not found"
            };
        }

        if (!bookingRequest.CanRetry)
        {
            return new BookingResult
            {
                Success = false,
                ErrorMessage = "Booking request cannot be retried"
            };
        }

        try
        {
            var originalRequest = JsonSerializer.Deserialize<CreateBookingRequest>(bookingRequest.RequestData);
            if (originalRequest == null)
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "Invalid booking request data"
                };
            }

            bookingRequest.MarkRetrying();
            await _context.SaveChangesAsync(cancellationToken);

            return await ProcessNewBooking(originalRequest, bookingRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying booking {IdempotencyKey}", idempotencyKey);

            bookingRequest.MarkFailed($"Retry failed: {ex.Message}");
            await _context.SaveChangesAsync(cancellationToken);

            return new BookingResult
            {
                Success = false,
                ErrorMessage = "Unable to retry booking"
            };
        }
    }

    private Task<BookingResult> HandleExistingRequest(BookingRequest existingRequest, string requestHash)
    {
        // Check if request hash matches (same request)
        if (existingRequest.RequestHash != requestHash)
        {
            return Task.FromResult(new BookingResult
            {
                Success = false,
                ErrorMessage = "Idempotency key already used with different request data"
            });
        }

        // Return existing result if completed
        if (existingRequest.IsCompleted && existingRequest.Booking != null)
        {
            return Task.FromResult(new BookingResult
            {
                Success = true,
                BookingId = existingRequest.Booking.Id,
                BookingReference = existingRequest.Booking.GetDisplayReference(),
                TotalAmount = existingRequest.Booking.TotalAmount,
                ExpiresAt = existingRequest.Booking.ExpiresAt,
                Message = "Booking already exists"
            });
        }

        // Return error if failed and cannot retry
        if (existingRequest.IsFailed && !existingRequest.CanRetry)
        {
            return Task.FromResult(new BookingResult
            {
                Success = false,
                ErrorMessage = existingRequest.ErrorMessage ?? "Booking request failed"
            });
        }

        // If still processing or can retry, return processing status
        return Task.FromResult(new BookingResult
        {
            Success = false,
            ErrorMessage = "Booking request is being processed"
        });
    }

    private async Task<BookingResult> ProcessNewBooking(
        CreateBookingRequest request,
        BookingRequest bookingRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            // First, hold the seats
            var holdRequest = new SeatHoldRequest
            {
                FlightId = request.FlightId,
                SeatIds = request.SeatIds,
                UserId = request.UserId?.ToString() ?? request.GuestId ?? "anonymous"
            };

            var holdResult = await HoldSeatsAsync(holdRequest, cancellationToken);
            if (!holdResult.Success)
            {
                bookingRequest.MarkFailed(holdResult.ErrorMessage ?? "Failed to hold seats");
                await _context.SaveChangesAsync(cancellationToken);
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = holdResult.ErrorMessage
                };
            }

            // Then confirm the booking
            var confirmRequest = new ConfirmBookingRequest
            {
                IdempotencyKey = request.IdempotencyKey,
                HoldReferences = holdResult.HoldReferences,
                UserId = request.UserId,
                GuestId = request.GuestId,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Passengers = request.Passengers
            };

            var bookingResult = await ConfirmBookingAsync(confirmRequest, cancellationToken);

            if (bookingResult.Success)
            {
                bookingRequest.MarkCompleted(bookingResult.BookingId!.Value, JsonSerializer.Serialize(bookingResult));
            }
            else
            {
                // Compensation: Release the held seats
                await ReleaseSeatsAsync(new ReleaseSeatRequest
                {
                    HoldReferences = holdResult.HoldReferences,
                    Reason = "Booking confirmation failed"
                }, cancellationToken);

                bookingRequest.MarkFailed(bookingResult.ErrorMessage ?? "Booking confirmation failed");
            }

            await _context.SaveChangesAsync(cancellationToken);
            return bookingResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing new booking");

            bookingRequest.MarkFailed($"Processing error: {ex.Message}");
            await _context.SaveChangesAsync(cancellationToken);

            return new BookingResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing your booking"
            };
        }
    }

    private static string ComputeRequestHash(CreateBookingRequest request)
    {
        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(requestJson));
        return Convert.ToBase64String(hashBytes);
    }
}

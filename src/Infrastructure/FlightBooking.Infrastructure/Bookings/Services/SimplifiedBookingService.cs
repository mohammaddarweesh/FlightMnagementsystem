using FlightBooking.Application.Bookings.Commands;
using FlightBooking.Application.Bookings.Queries;
using FlightBooking.Application.Bookings.Services;
using FlightBooking.Domain.Bookings;
using FlightBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Use aliases to resolve ambiguity
using CreateResult = FlightBooking.Application.Bookings.Commands.CreateBookingResult;
using ModifyResult = FlightBooking.Application.Bookings.Commands.ModifyBookingResult;
using CancelResult = FlightBooking.Application.Bookings.Commands.CancelBookingResult;
using ConfirmResult = FlightBooking.Application.Bookings.Commands.ConfirmBookingResult;
using CheckInResult = FlightBooking.Application.Bookings.Commands.CheckInResult;
using ValidationResult = FlightBooking.Application.Bookings.Queries.BookingModificationValidationResult;

namespace FlightBooking.Infrastructure.Bookings.Services;

public class SimplifiedBookingService : IBookingService
{
    private readonly ILogger<SimplifiedBookingService> _logger;
    private readonly ApplicationDbContext _context;
    private static readonly Dictionary<string, object> _idempotencyCache = new();

    public SimplifiedBookingService(ILogger<SimplifiedBookingService> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<CreateResult> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken = default)
    {
        // Check idempotency
        if (_idempotencyCache.TryGetValue(command.IdempotencyKey, out var cachedResult))
        {
            return (CreateResult)cachedResult;
        }

        try
        {
            var bookingId = Guid.NewGuid();
            var bookingReference = GenerateBookingReference();
            
            var booking = new Booking
            {
                Id = bookingId,
                BookingReference = bookingReference,
                Status = BookingStatus.Draft,
                FlightId = command.FlightId,
                UserId = command.CustomerId,
                GuestId = command.GuestId,
                ContactEmail = command.ContactInfo.Email,
                ContactPhone = command.ContactInfo.Phone,
                TotalAmount = 500m, // Simplified pricing
                Currency = "USD",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                PaymentStatus = PaymentStatus.Pending.ToString()
            };

            // Save to database
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(cancellationToken);

            var result = new CreateResult
            {
                Success = true,
                BookingId = bookingId,
                BookingReference = bookingReference,
                TotalAmount = booking.TotalAmount,
                ExpiresAt = booking.ExpiresAt
            };

            _idempotencyCache[command.IdempotencyKey] = result;

            _logger.LogInformation("Created booking {BookingReference} for {PassengerCount} passengers", 
                bookingReference, command.Passengers.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return new CreateResult
            {
                Success = false,
                ErrorMessage = "An error occurred while creating the booking"
            };
        }
    }

    public async Task<ModifyResult> ModifyBookingAsync(ModifyBookingCommand command, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings.FindAsync(command.BookingId, cancellationToken);
        if (booking == null)
        {
            return new ModifyResult
            {
                Success = false,
                ErrorMessage = "Booking not found"
            };
        }

        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
        {
            return new ModifyResult
            {
                Success = false,
                ErrorMessage = $"Cannot modify booking in status: {booking.Status}"
            };
        }

        // Simplified modification logic
        var additionalCost = command.ModificationType switch
        {
            BookingModificationType.DatesChanged => 150m,
            BookingModificationType.SeatChanged => 25m,
            BookingModificationType.FareClassUpgraded => 200m,
            _ => 0m
        };

        booking.TotalAmount += additionalCost;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Modified booking {BookingReference}: {ModificationType}",
            booking.BookingReference, command.ModificationType);

        return new ModifyResult
        {
            Success = true,
            AdditionalCost = additionalCost > 0 ? additionalCost : null,
            RequiresPayment = additionalCost > 0
        };
    }

    public async Task<CancelResult> CancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings.FindAsync(command.BookingId, cancellationToken);
        if (booking == null)
        {
            return new CancelResult
            {
                Success = false,
                ErrorMessage = "Booking not found"
            };
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return new CancelResult
            {
                Success = false,
                ErrorMessage = "Booking is already cancelled"
            };
        }

        // Simplified cancellation logic
        var cancellationFee = booking.TotalAmount * 0.25m; // 25% cancellation fee
        var processingFee = 25m;
        var refundAmount = Math.Max(0, booking.TotalAmount - cancellationFee - processingFee);

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled booking {BookingReference}. Refund: {RefundAmount:C}",
            booking.BookingReference, refundAmount);

        return new CancelResult
        {
            Success = true,
            RefundAmount = refundAmount,
            CancellationFee = cancellationFee,
            ProcessingFee = processingFee,
            IsRefundEligible = refundAmount > 0
        };
    }

    public async Task<ConfirmResult> ConfirmBookingAsync(ConfirmBookingCommand command, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings.FindAsync(command.BookingId, cancellationToken);
        if (booking == null)
        {
            return new ConfirmResult
            {
                Success = false,
                ErrorMessage = "Booking not found"
            };
        }

        if (booking.Status != BookingStatus.PaymentPending)
        {
            return new ConfirmResult
            {
                Success = false,
                ErrorMessage = $"Cannot confirm booking in status: {booking.Status}"
            };
        }

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = DateTime.UtcNow;
        booking.PaymentStatus = PaymentStatus.Completed.ToString();
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Confirmed booking {BookingReference}", booking.BookingReference);

        return new ConfirmResult
        {
            Success = true,
            BookingReference = booking.BookingReference,
            ConfirmedAt = booking.ConfirmedAt,
            EmailSent = true
        };
    }

    public async Task<CheckInResult> CheckInAsync(CheckInCommand command, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings.FindAsync(command.BookingId, cancellationToken);
        if (booking == null)
        {
            return new CheckInResult
            {
                Success = false,
                ErrorMessage = "Booking not found"
            };
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            return new CheckInResult
            {
                Success = false,
                ErrorMessage = $"Cannot check in booking in status: {booking.Status}"
            };
        }

        booking.Status = BookingStatus.CheckedIn;
        booking.UpdatedAt = DateTime.UtcNow;

        var boardingPasses = command.PassengerReferences.Select(passengerRef => new BoardingPass
        {
            PassengerName = $"Passenger {passengerRef}",
            SeatNumber = "12A",
            BoardingGroup = "Group 3",
            QRCode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{booking.BookingReference}:{passengerRef}")),
            BoardingTime = DateTime.UtcNow.AddMinutes(30),
            Gate = "A12"
        }).ToList();

        _logger.LogInformation("Checked in {PassengerCount} passengers for booking {BookingReference}", 
            command.PassengerReferences.Count, booking.BookingReference);

        return new CheckInResult
        {
            Success = true,
            BoardingPasses = boardingPasses,
            CheckInTime = DateTime.UtcNow,
            Gate = "A12",
            BoardingTime = DateTime.UtcNow.AddMinutes(30)
        };
    }

    public async Task<ValidationResult> ValidateModificationAsync(ValidateBookingModificationQuery query, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings.FindAsync(query.BookingId, cancellationToken);
        if (booking == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Booking not found" }
            };
        }

        var estimatedCost = query.ModificationType switch
        {
            BookingModificationType.DatesChanged => 150m,
            BookingModificationType.SeatChanged => 25m,
            BookingModificationType.FareClassUpgraded => 200m,
            _ => 0m
        };

        return new ValidationResult
        {
            IsValid = true,
            EstimatedCost = estimatedCost,
            RequiresApproval = false,
            Deadline = DateTime.UtcNow.AddHours(2)
        };
    }

    public async Task<CancellationCalculationResult> CalculateCancellationAsync(Guid bookingId, CancellationReason reason, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings.FindAsync(bookingId, cancellationToken);
        if (booking == null)
        {
            throw new Exception("Booking not found");
        }

        var cancellationFee = booking.TotalAmount * 0.25m;
        var processingFee = 25m;
        var refundAmount = Math.Max(0, booking.TotalAmount - cancellationFee - processingFee);

        return new CancellationCalculationResult
        {
            RefundAmount = refundAmount,
            CancellationFee = cancellationFee,
            ProcessingFee = processingFee,
            TotalDeductions = cancellationFee + processingFee,
            IsEligibleForRefund = refundAmount > 0,
            Summary = $"Refund: {refundAmount:C}, Fees: {cancellationFee + processingFee:C}"
        };
    }

    public async Task<int> ExpireBookingsAsync(CancellationToken cancellationToken = default)
    {
        var expiredCount = 0;
        var cutoffTime = DateTime.UtcNow;

        var expiredBookings = await _context.Bookings
            .Where(b => b.ExpiresAt <= cutoffTime && b.Status == BookingStatus.PaymentPending)
            .ToListAsync(cancellationToken);

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatus.Expired;
            booking.UpdatedAt = DateTime.UtcNow;
            expiredCount++;
        }

        if (expiredCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Expired {ExpiredCount} bookings", expiredCount);
        return expiredCount;
    }

    public async Task<bool> SendBookingConfirmationAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending booking confirmation for {BookingId}", bookingId);
        return true;
    }

    public async Task<int> SendBookingRemindersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending booking reminders");
        return 0;
    }

    public async Task<int> ProcessAutoCheckInAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing auto check-in");
        return 0;
    }

    public async Task<string> GenerateBookingReferenceAsync(CancellationToken cancellationToken = default)
    {
        return GenerateBookingReference();
    }

    private string GenerateBookingReference()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

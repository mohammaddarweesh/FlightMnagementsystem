using FlightBooking.Application.Bookings.Commands;
using FlightBooking.Application.Bookings.Queries;
using FlightBooking.Domain.Bookings;

namespace FlightBooking.Application.Bookings.Services;

public interface IBookingService
{
    /// <summary>
    /// Creates a new booking with idempotency support
    /// </summary>
    Task<CreateBookingResult> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifies an existing booking with validation and state management
    /// </summary>
    Task<ModifyBookingResult> ModifyBookingAsync(ModifyBookingCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a booking with refund calculation
    /// </summary>
    Task<CancelBookingResult> CancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a booking after payment
    /// </summary>
    Task<ConfirmBookingResult> ConfirmBookingAsync(ConfirmBookingCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes check-in for passengers
    /// </summary>
    Task<CheckInResult> CheckInAsync(CheckInCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a booking modification is allowed
    /// </summary>
    Task<BookingModificationValidationResult> ValidateModificationAsync(ValidateBookingModificationQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates cancellation fees and refund amount
    /// </summary>
    Task<CancellationCalculationResult> CalculateCancellationAsync(Guid bookingId, CancellationReason reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires bookings that have passed their payment deadline
    /// </summary>
    Task<int> ExpireBookingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends booking confirmation notifications
    /// </summary>
    Task<bool> SendBookingConfirmationAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends booking reminder notifications
    /// </summary>
    Task<int> SendBookingRemindersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes automatic check-in for eligible bookings
    /// </summary>
    Task<int> ProcessAutoCheckInAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets booking reference generator
    /// </summary>
    Task<string> GenerateBookingReferenceAsync(CancellationToken cancellationToken = default);
}

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Booking?> GetByReferenceAsync(string bookingReference, CancellationToken cancellationToken = default);
    Task<List<Booking>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<List<Booking>> GetByGuestAsync(string guestId, CancellationToken cancellationToken = default);
    Task<List<Booking>> GetByFlightAsync(Guid flightId, CancellationToken cancellationToken = default);
    Task<List<Booking>> GetExpiringBookingsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
    Task<List<Booking>> GetBookingsForReminderAsync(DateTime reminderTime, CancellationToken cancellationToken = default);
    Task<List<Booking>> GetBookingsForAutoCheckInAsync(DateTime checkInTime, CancellationToken cancellationToken = default);
    Task<Booking> AddAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<Booking> UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Booking?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<Booking>> SearchAsync(BookingSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<int> CountAsync(BookingSearchCriteria criteria, CancellationToken cancellationToken = default);
}

public interface IBookingHistoryService
{
    Task<List<BookingHistoryDto>> GetCustomerHistoryAsync(Guid customerId, BookingHistoryFilter filter, CancellationToken cancellationToken = default);
    Task<List<BookingHistoryDto>> GetGuestHistoryAsync(string guestId, BookingHistoryFilter filter, CancellationToken cancellationToken = default);
    Task RecordBookingEventAsync(Guid bookingId, BookingEventType eventType, string description, string triggeredBy, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
    Task RecordBookingModificationAsync(Guid bookingId, BookingModificationType modificationType, string description, string modifiedBy, Dictionary<string, object>? previousValues = null, Dictionary<string, object>? newValues = null, CancellationToken cancellationToken = default);
    Task<List<BookingEvent>> GetBookingEventsAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<List<BookingModification>> GetBookingModificationsAsync(Guid bookingId, CancellationToken cancellationToken = default);
}

public interface IBookingNotificationService
{
    Task SendBookingConfirmationAsync(Booking booking, CancellationToken cancellationToken = default);
    Task SendBookingModificationAsync(Booking booking, BookingModificationType modificationType, string description, CancellationToken cancellationToken = default);
    Task SendBookingCancellationAsync(Booking booking, CancellationInfo cancellationInfo, CancellationToken cancellationToken = default);
    Task SendBookingReminderAsync(Booking booking, TimeSpan timeUntilDeparture, CancellationToken cancellationToken = default);
    Task SendCheckInReminderAsync(Booking booking, CancellationToken cancellationToken = default);
    Task SendCheckInConfirmationAsync(Booking booking, List<BoardingPass> boardingPasses, CancellationToken cancellationToken = default);
    Task SendPaymentReminderAsync(Booking booking, CancellationToken cancellationToken = default);
    Task SendBookingExpiryWarningAsync(Booking booking, TimeSpan timeUntilExpiry, CancellationToken cancellationToken = default);
}

public interface IBookingValidationService
{
    Task<BookingValidationResult> ValidateCreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken = default);
    Task<BookingValidationResult> ValidateModifyBookingAsync(ModifyBookingCommand command, CancellationToken cancellationToken = default);
    Task<BookingValidationResult> ValidateCancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken = default);
    Task<BookingValidationResult> ValidateConfirmBookingAsync(ConfirmBookingCommand command, CancellationToken cancellationToken = default);
    Task<BookingValidationResult> ValidateCheckInAsync(CheckInCommand command, CancellationToken cancellationToken = default);
    Task<bool> ValidatePassengerDocumentsAsync(List<PassengerInfo> passengers, string route, CancellationToken cancellationToken = default);
    Task<bool> ValidateSeatAvailabilityAsync(Guid flightId, List<SeatSelectionDto> seatSelections, CancellationToken cancellationToken = default);
    Task<bool> ValidateFlightAvailabilityAsync(Guid flightId, int passengerCount, CancellationToken cancellationToken = default);
}

public interface IBookingPricingService
{
    Task<BookingPricingResult> CalculateBookingPricingAsync(CreateBookingCommand command, CancellationToken cancellationToken = default);
    Task<BookingPricingResult> RecalculatePricingAsync(Guid bookingId, List<BookingModification> modifications, CancellationToken cancellationToken = default);
    Task<decimal> CalculateModificationCostAsync(Booking booking, BookingModificationType modificationType, Dictionary<string, object> modificationData, CancellationToken cancellationToken = default);
    Task<CancellationCalculationResult> CalculateCancellationAsync(Booking booking, CancellationReason reason, CancellationToken cancellationToken = default);
}

public interface IBookingIdempotencyService
{
    Task<bool> IsProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<T?> GetResultAsync<T>(string idempotencyKey, CancellationToken cancellationToken = default) where T : class;
    Task StoreResultAsync<T>(string idempotencyKey, T result, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task InvalidateAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

// Supporting classes and records
public record BookingSearchCriteria
{
    public string? BookingReference { get; init; }
    public string? PassengerName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public Guid? FlightId { get; init; }
    public DateTime? DepartureDate { get; init; }
    public List<BookingStatus>? Statuses { get; init; }
    public DateTime? BookedFrom { get; init; }
    public DateTime? BookedTo { get; init; }
    public Guid? CustomerId { get; init; }
    public string? GuestId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "BookedAt";
    public bool SortDescending { get; init; } = true;
}

public record BookingHistoryFilter
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public List<BookingStatus>? Statuses { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "BookedAt";
    public bool SortDescending { get; init; } = true;
}

public record BookingValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record BookingPricingResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public BookingPricing Pricing { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record CancellationCalculationResult
{
    public decimal RefundAmount { get; init; }
    public decimal CancellationFee { get; init; }
    public decimal ProcessingFee { get; init; }
    public decimal TotalDeductions { get; init; }
    public bool IsEligibleForRefund { get; init; }
    public List<string> RefundConditions { get; init; } = new();
    public string Summary { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public class BookingNotFoundException : Exception
{
    public Guid? BookingId { get; }
    public string? BookingReference { get; }

    public BookingNotFoundException(Guid bookingId) : base($"Booking with ID {bookingId} not found")
    {
        BookingId = bookingId;
    }

    public BookingNotFoundException(string bookingReference) : base($"Booking with reference {bookingReference} not found")
    {
        BookingReference = bookingReference;
    }
}

public class BookingStateException : Exception
{
    public Guid BookingId { get; }
    public BookingStatus CurrentStatus { get; }
    public string Operation { get; }

    public BookingStateException(Guid bookingId, BookingStatus currentStatus, string operation, string message) 
        : base(message)
    {
        BookingId = bookingId;
        CurrentStatus = currentStatus;
        Operation = operation;
    }
}

public class BookingConcurrencyException : Exception
{
    public Guid BookingId { get; }
    public string Operation { get; }

    public BookingConcurrencyException(Guid bookingId, string operation, string message) 
        : base(message)
    {
        BookingId = bookingId;
        Operation = operation;
    }

    public BookingConcurrencyException(Guid bookingId, string operation, string message, Exception innerException) 
        : base(message, innerException)
    {
        BookingId = bookingId;
        Operation = operation;
    }
}

public class BookingValidationException : Exception
{
    public List<string> ValidationErrors { get; }

    public BookingValidationException(List<string> validationErrors) 
        : base($"Booking validation failed: {string.Join("; ", validationErrors)}")
    {
        ValidationErrors = validationErrors;
    }

    public BookingValidationException(string validationError) 
        : base($"Booking validation failed: {validationError}")
    {
        ValidationErrors = new List<string> { validationError };
    }
}





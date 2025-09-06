using FlightBooking.Application.Common.Interfaces;
using FlightBooking.Domain.Bookings;
using MediatR;
using FluentValidation;

namespace FlightBooking.Application.Bookings.Commands;

public record CreateBookingCommand : IRequest<CreateBookingResult>
{
    public string IdempotencyKey { get; init; } = string.Empty;
    public Guid FlightId { get; init; }
    public Guid FareClassId { get; init; }
    public Guid? CustomerId { get; init; }
    public string? GuestId { get; init; }
    public ContactInfoDto ContactInfo { get; init; } = new();
    public List<PassengerInfoDto> Passengers { get; init; } = new();
    public List<SeatSelectionDto> SeatSelections { get; init; } = new();
    public List<ExtraServiceDto> ExtraServices { get; init; } = new();
    public string? PromoCode { get; init; }
    public PaymentInfoDto? PaymentInfo { get; init; }
    public bool AutoConfirm { get; init; } = false;
    public string CreatedBy { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record CreateBookingResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? BookingId { get; init; }
    public string? BookingReference { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required");

        RuleFor(x => x.FlightId)
            .NotEmpty()
            .WithMessage("Flight ID is required");

        RuleFor(x => x.FareClassId)
            .NotEmpty()
            .WithMessage("Fare class ID is required");

        RuleFor(x => x.ContactInfo)
            .NotNull()
            .WithMessage("Contact information is required");

        RuleFor(x => x.ContactInfo.Email)
            .NotEmpty()
            .EmailAddress()
            .When(x => x.ContactInfo != null)
            .WithMessage("Valid email address is required");

        RuleFor(x => x.ContactInfo.Phone)
            .NotEmpty()
            .When(x => x.ContactInfo != null)
            .WithMessage("Phone number is required");

        RuleFor(x => x.Passengers)
            .NotEmpty()
            .WithMessage("At least one passenger is required");

        RuleFor(x => x.Passengers)
            .Must(passengers => passengers.Count <= 9)
            .WithMessage("Maximum 9 passengers allowed per booking");

        RuleForEach(x => x.Passengers)
            .SetValidator(new PassengerInfoDtoValidator());

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("Created by is required");

        // Custom validation rules
        RuleFor(x => x)
            .Must(HaveValidSeatSelections)
            .WithMessage("Seat selections must match passenger count");

        RuleFor(x => x)
            .Must(HaveUniquePassengerReferences)
            .WithMessage("Passenger references must be unique");
    }

    private bool HaveValidSeatSelections(CreateBookingCommand command)
    {
        if (!command.SeatSelections.Any())
            return true; // Seat selection is optional

        // Each passenger can have at most one seat
        var passengerReferences = command.Passengers.Select(p => p.PassengerReference).ToList();
        var seatPassengerReferences = command.SeatSelections.Select(s => s.PassengerReference).ToList();

        return seatPassengerReferences.All(passengerReferences.Contains);
    }

    private bool HaveUniquePassengerReferences(CreateBookingCommand command)
    {
        var references = command.Passengers.Select(p => p.PassengerReference).ToList();
        return references.Count == references.Distinct().Count();
    }
}

public class PassengerInfoDtoValidator : AbstractValidator<PassengerInfoDto>
{
    public PassengerInfoDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("First name is required and must be less than 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Last name is required and must be less than 50 characters");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.Today)
            .WithMessage("Date of birth must be in the past");

        RuleFor(x => x.Gender)
            .IsInEnum()
            .WithMessage("Valid gender is required");

        RuleFor(x => x.Passport)
            .NotNull()
            .When(x => x.Type == PassengerType.Adult)
            .WithMessage("Passport information is required for adult passengers");

        RuleFor(x => x.Passport!.Number)
            .NotEmpty()
            .When(x => x.Passport != null)
            .WithMessage("Passport number is required");

        RuleFor(x => x.Passport!.ExpiryDate)
            .GreaterThan(DateTime.Today.AddMonths(6))
            .When(x => x.Passport != null)
            .WithMessage("Passport must be valid for at least 6 months");
    }
}

public record ModifyBookingCommand : IRequest<ModifyBookingResult>
{
    public Guid BookingId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public BookingModificationType ModificationType { get; init; }
    public Dictionary<string, object> ModificationData { get; init; } = new();
    public string ModifiedBy { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public bool ForceModification { get; init; } = false;
}

public record ModifyBookingResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal? AdditionalCost { get; init; }
    public decimal? RefundAmount { get; init; }
    public bool RequiresPayment { get; init; }
    public bool RequiresApproval { get; init; }
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record CancelBookingCommand : IRequest<CancelBookingResult>
{
    public Guid BookingId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public CancellationReason Reason { get; init; }
    public string? ReasonDescription { get; init; }
    public string CancelledBy { get; init; } = string.Empty;
    public bool ProcessRefundImmediately { get; init; } = false;
}

public record CancelBookingResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal RefundAmount { get; init; }
    public decimal CancellationFee { get; init; }
    public decimal ProcessingFee { get; init; }
    public bool IsRefundEligible { get; init; }
    public DateTime? RefundProcessedAt { get; init; }
    public string? RefundTransactionId { get; init; }
    public List<string> Warnings { get; init; } = new();
}

public record ConfirmBookingCommand : IRequest<ConfirmBookingResult>
{
    public Guid BookingId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string PaymentIntentId { get; init; } = string.Empty;
    public string ConfirmedBy { get; init; } = string.Empty;
    public bool SendConfirmationEmail { get; init; } = true;
}

public record ConfirmBookingResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? BookingReference { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public bool EmailSent { get; init; }
    public List<string> Warnings { get; init; } = new();
}

public record CheckInCommand : IRequest<CheckInResult>
{
    public Guid BookingId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public List<string> PassengerReferences { get; init; } = new();
    public Dictionary<string, string> SeatPreferences { get; init; } = new();
    public string CheckedInBy { get; init; } = string.Empty;
    public bool AcceptTerms { get; init; }
}

public record CheckInResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public List<BoardingPass> BoardingPasses { get; init; } = new();
    public DateTime CheckInTime { get; init; }
    public string? Gate { get; init; }
    public DateTime BoardingTime { get; init; }
    public List<string> Warnings { get; init; } = new();
}

public record BoardingPass
{
    public string PassengerName { get; init; } = string.Empty;
    public string SeatNumber { get; init; } = string.Empty;
    public string BoardingGroup { get; init; } = string.Empty;
    public string QRCode { get; init; } = string.Empty;
    public DateTime BoardingTime { get; init; }
    public string Gate { get; init; } = string.Empty;
}

// DTOs
public record ContactInfoDto
{
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? AlternatePhone { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }
}

public record PassengerInfoDto
{
    public string PassengerReference { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public PassengerType Type { get; init; }
    public PassportInfoDto? Passport { get; init; }
    public string? SpecialRequests { get; init; }
    public string? FrequentFlyerNumber { get; init; }
    public string? KnownTravelerNumber { get; init; }
    public bool RequiresSpecialAssistance { get; init; }
    public List<string> DietaryRestrictions { get; init; } = new();
}

public record PassportInfoDto
{
    public string Number { get; init; } = string.Empty;
    public string IssuingCountry { get; init; } = string.Empty;
    public string Nationality { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; }
    public DateTime ExpiryDate { get; init; }
}

public record SeatSelectionDto
{
    public string PassengerReference { get; init; } = string.Empty;
    public string SeatNumber { get; init; } = string.Empty;
    public SeatType SeatType { get; init; }
    public decimal SeatFee { get; init; }
}

public record ExtraServiceDto
{
    public string ServiceCode { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public ExtraServiceType Type { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; } = 1;
    public string? PassengerReference { get; init; }
}

public record PaymentInfoDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public PaymentMethod Method { get; init; }
    public string? PaymentIntentId { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

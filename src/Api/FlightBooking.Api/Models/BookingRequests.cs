using FlightBooking.Domain.Bookings;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Api.Models;

public record ModifyBookingRequest
{
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    [Required]
    public BookingModificationType ModificationType { get; init; }

    [Required]
    public Dictionary<string, object> ModificationData { get; init; } = new();

    public string? Reason { get; init; }

    public bool ForceModification { get; init; } = false;
}

public record CancelBookingRequest
{
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    [Required]
    public CancellationReason Reason { get; init; }

    public string? ReasonDescription { get; init; }

    public bool ProcessRefundImmediately { get; init; } = false;
}

public record ConfirmBookingRequest
{
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    [Required]
    public string PaymentIntentId { get; init; } = string.Empty;

    public bool SendConfirmationEmail { get; init; } = true;
}

public record CheckInRequest
{
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one passenger must be selected for check-in")]
    public List<string> PassengerReferences { get; init; } = new();

    public Dictionary<string, string> SeatPreferences { get; init; } = new();

    [Required]
    public bool AcceptTerms { get; init; }
}

public record ValidateModificationRequest
{
    [Required]
    public BookingModificationType ModificationType { get; init; }

    [Required]
    public Dictionary<string, object> ModificationData { get; init; } = new();
}

public record CalculateCancellationRequest
{
    [Required]
    public CancellationReason Reason { get; init; }
}

public record BookingHistoryRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public List<BookingStatus>? Statuses { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = "BookedAt";
    public bool SortDescending { get; init; } = true;
}

public record BookingSearchRequest
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

    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = "BookedAt";
    public bool SortDescending { get; init; } = true;
}

// Specific modification request types for better validation
public record ChangeDatesRequest : ModifyBookingRequest
{
    [Required]
    public DateTime NewDepartureDate { get; init; }

    public DateTime? NewReturnDate { get; init; }

    public ChangeDatesRequest()
    {
        ModificationType = BookingModificationType.DatesChanged;
        ModificationData = new Dictionary<string, object>();
    }

    public ChangeDatesRequest(DateTime newDepartureDate, DateTime? newReturnDate = null, string? reason = null)
    {
        ModificationType = BookingModificationType.DatesChanged;
        NewDepartureDate = newDepartureDate;
        NewReturnDate = newReturnDate;
        Reason = reason;
        ModificationData = new Dictionary<string, object>
        {
            ["departure_date"] = newDepartureDate
        };
        
        if (newReturnDate.HasValue)
        {
            ModificationData["return_date"] = newReturnDate.Value;
        }
    }
}

public record UpdateContactRequest : ModifyBookingRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; init; } = string.Empty;

    public string? AlternatePhone { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }

    public UpdateContactRequest()
    {
        ModificationType = BookingModificationType.ContactUpdated;
        ModificationData = new Dictionary<string, object>();
    }

    public UpdateContactRequest(string email, string phone, string? alternatePhone = null, string? emergencyContact = null, string? emergencyPhone = null)
    {
        ModificationType = BookingModificationType.ContactUpdated;
        Email = email;
        Phone = phone;
        AlternatePhone = alternatePhone;
        EmergencyContact = emergencyContact;
        EmergencyPhone = emergencyPhone;
        ModificationData = new Dictionary<string, object>
        {
            ["contact_info"] = new
            {
                Email = email,
                Phone = phone,
                AlternatePhone = alternatePhone,
                EmergencyContact = emergencyContact,
                EmergencyPhone = emergencyPhone
            }
        };
    }
}

public record ChangeSeatRequest : ModifyBookingRequest
{
    [Required]
    public string PassengerReference { get; init; } = string.Empty;

    [Required]
    public string SeatNumber { get; init; } = string.Empty;

    public decimal SeatFee { get; init; }

    public ChangeSeatRequest()
    {
        ModificationType = BookingModificationType.SeatChanged;
        ModificationData = new Dictionary<string, object>();
    }

    public ChangeSeatRequest(string passengerReference, string seatNumber, decimal seatFee = 0)
    {
        ModificationType = BookingModificationType.SeatChanged;
        PassengerReference = passengerReference;
        SeatNumber = seatNumber;
        SeatFee = seatFee;
        ModificationData = new Dictionary<string, object>
        {
            ["passenger_reference"] = passengerReference,
            ["seat_number"] = seatNumber,
            ["seat_fee"] = seatFee
        };
    }
}

public record UpgradeFareClassRequest : ModifyBookingRequest
{
    [Required]
    public Guid NewFareClassId { get; init; }

    [Required]
    public string NewFareClassName { get; init; } = string.Empty;

    public decimal UpgradeFee { get; init; }

    public UpgradeFareClassRequest()
    {
        ModificationType = BookingModificationType.FareClassUpgraded;
        ModificationData = new Dictionary<string, object>();
    }

    public UpgradeFareClassRequest(Guid newFareClassId, string newFareClassName, decimal upgradeFee)
    {
        ModificationType = BookingModificationType.FareClassUpgraded;
        NewFareClassId = newFareClassId;
        NewFareClassName = newFareClassName;
        UpgradeFee = upgradeFee;
        ModificationData = new Dictionary<string, object>
        {
            ["new_fare_class_id"] = newFareClassId,
            ["new_fare_class_name"] = newFareClassName,
            ["upgrade_fee"] = upgradeFee
        };
    }
}

public record AddExtraServiceRequest : ModifyBookingRequest
{
    [Required]
    public string ServiceCode { get; init; } = string.Empty;

    [Required]
    public string ServiceName { get; init; } = string.Empty;

    [Required]
    public ExtraServiceType ServiceType { get; init; }

    public decimal UnitPrice { get; init; }

    [Range(1, 10, ErrorMessage = "Quantity must be between 1 and 10")]
    public int Quantity { get; init; } = 1;

    public string? PassengerReference { get; init; }

    public AddExtraServiceRequest()
    {
        ModificationType = BookingModificationType.ExtraAdded;
        ModificationData = new Dictionary<string, object>();
    }

    public AddExtraServiceRequest(string serviceCode, string serviceName, ExtraServiceType serviceType, decimal unitPrice, int quantity = 1, string? passengerReference = null)
    {
        ModificationType = BookingModificationType.ExtraAdded;
        ServiceCode = serviceCode;
        ServiceName = serviceName;
        ServiceType = serviceType;
        UnitPrice = unitPrice;
        Quantity = quantity;
        PassengerReference = passengerReference;
        ModificationData = new Dictionary<string, object>
        {
            ["service_code"] = serviceCode,
            ["service_name"] = serviceName,
            ["service_type"] = serviceType,
            ["unit_price"] = unitPrice,
            ["quantity"] = quantity,
            ["passenger_reference"] = passengerReference ?? string.Empty
        };
    }
}

// Custom validation attributes
public class PhoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string phone || string.IsNullOrWhiteSpace(phone))
            return false;

        // Simple phone validation - in production, use a proper phone validation library
        return phone.Length >= 10 && phone.All(c => char.IsDigit(c) || c == '+' || c == '-' || c == ' ' || c == '(' || c == ')');
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid phone number.";
    }
}

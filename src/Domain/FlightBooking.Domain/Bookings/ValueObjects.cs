using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Bookings;

public record ContactInfo
{
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? AlternatePhone { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }

    public static ContactInfo Create(string email, string phone, string? alternatePhone = null, 
        string? emergencyContact = null, string? emergencyPhone = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required", nameof(phone));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        return new ContactInfo
        {
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone.Trim(),
            AlternatePhone = alternatePhone?.Trim(),
            EmergencyContact = emergencyContact?.Trim(),
            EmergencyPhone = emergencyPhone?.Trim()
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public record PassengerInfo
{
    public string PassengerReference { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public PassengerType Type { get; init; }
    public PassportInfo? Passport { get; init; }
    public string? SpecialRequests { get; init; }
    public string? FrequentFlyerNumber { get; init; }
    public string? KnownTravelerNumber { get; init; }
    public bool RequiresSpecialAssistance { get; init; }
    public List<string> DietaryRestrictions { get; init; } = new();

    public string FullName => string.IsNullOrWhiteSpace(MiddleName) 
        ? $"{FirstName} {LastName}" 
        : $"{FirstName} {MiddleName} {LastName}";

    public int Age => DateTime.Today.Year - DateOfBirth.Year - 
                     (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

    public bool IsInfant => Age < 2;
    public bool IsChild => Age >= 2 && Age < 12;
    public bool IsAdult => Age >= 12;

    public static PassengerInfo Create(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        Gender gender,
        PassengerType type = PassengerType.Adult,
        string? middleName = null,
        PassportInfo? passport = null,
        string? specialRequests = null,
        string? frequentFlyerNumber = null,
        string? knownTravelerNumber = null,
        bool requiresSpecialAssistance = false,
        List<string>? dietaryRestrictions = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        if (dateOfBirth > DateTime.Today)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(dateOfBirth));

        var passengerReference = GeneratePassengerReference();

        return new PassengerInfo
        {
            PassengerReference = passengerReference,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            MiddleName = middleName?.Trim(),
            DateOfBirth = dateOfBirth,
            Gender = gender,
            Type = type,
            Passport = passport,
            SpecialRequests = specialRequests?.Trim(),
            FrequentFlyerNumber = frequentFlyerNumber?.Trim(),
            KnownTravelerNumber = knownTravelerNumber?.Trim(),
            RequiresSpecialAssistance = requiresSpecialAssistance,
            DietaryRestrictions = dietaryRestrictions ?? new List<string>()
        };
    }

    private static string GeneratePassengerReference()
    {
        return $"PAX{DateTime.UtcNow.Ticks % 1000000:D6}";
    }
}

public record PassportInfo
{
    public string Number { get; init; } = string.Empty;
    public string IssuingCountry { get; init; } = string.Empty;
    public string Nationality { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; }
    public DateTime ExpiryDate { get; init; }

    public bool IsValid => ExpiryDate > DateTime.Today.AddMonths(6); // 6 months validity required
    public bool IsExpired => ExpiryDate <= DateTime.Today;
    public int DaysUntilExpiry => (ExpiryDate - DateTime.Today).Days;

    public static PassportInfo Create(
        string number,
        string issuingCountry,
        string nationality,
        DateTime issueDate,
        DateTime expiryDate)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Passport number is required", nameof(number));
        
        if (string.IsNullOrWhiteSpace(issuingCountry))
            throw new ArgumentException("Issuing country is required", nameof(issuingCountry));

        if (string.IsNullOrWhiteSpace(nationality))
            throw new ArgumentException("Nationality is required", nameof(nationality));

        if (expiryDate <= issueDate)
            throw new ArgumentException("Expiry date must be after issue date", nameof(expiryDate));

        return new PassportInfo
        {
            Number = number.Trim().ToUpperInvariant(),
            IssuingCountry = issuingCountry.Trim().ToUpperInvariant(),
            Nationality = nationality.Trim().ToUpperInvariant(),
            IssueDate = issueDate,
            ExpiryDate = expiryDate
        };
    }
}

public record SeatAssignment
{
    public string PassengerReference { get; init; } = string.Empty;
    public string SeatNumber { get; init; } = string.Empty;
    public SeatType SeatType { get; init; }
    public decimal SeatFee { get; init; }
    public string? SeatFeatures { get; init; }
    public DateTime AssignedAt { get; init; }
    public string AssignedBy { get; init; } = string.Empty;

    public bool IsWindowSeat => SeatNumber.EndsWith('A') || SeatNumber.EndsWith('F');
    public bool IsAisleSeat => SeatNumber.EndsWith('C') || SeatNumber.EndsWith('D');
    public bool IsMiddleSeat => SeatNumber.EndsWith('B') || SeatNumber.EndsWith('E');
}

public record BookingExtra
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ServiceCode { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ExtraServiceType Type { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal TotalPrice => UnitPrice * Quantity;
    public string? PassengerReference { get; init; }
    public bool IsRefundable { get; init; }
    public string? Terms { get; init; }
    public DateTime AddedAt { get; init; } = DateTime.UtcNow;
    public string AddedBy { get; init; } = string.Empty;
}

public record BookingPricing
{
    public decimal BaseFare { get; init; }
    public decimal Taxes { get; init; }
    public decimal Fees { get; init; }
    public decimal Extras { get; init; }
    public decimal Discounts { get; init; }
    public decimal UpgradeFees { get; init; }
    public decimal GrandTotal => BaseFare + Taxes + Fees + Extras + UpgradeFees - Discounts;
    public string Currency { get; init; } = "USD";
    public List<PricingComponent> Components { get; init; } = new();

    public BookingPricing WithUpgradeFee(decimal upgradeFee)
    {
        return this with { UpgradeFees = UpgradeFees + upgradeFee };
    }

    public BookingPricing WithExtra(decimal extraAmount)
    {
        return this with { Extras = Extras + extraAmount };
    }

    public BookingPricing WithDiscount(decimal discountAmount)
    {
        return this with { Discounts = Discounts + discountAmount };
    }
}

public record PricingComponent
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public PricingComponentType Type { get; init; }
    public bool IsRefundable { get; init; }
}

public record PaymentInfo
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public PaymentMethod Method { get; init; }
    public PaymentStatus Status { get; init; }
    public string? TransactionId { get; init; }
    public string? PaymentIntentId { get; init; }
    public DateTime ProcessedAt { get; init; }
    public string? FailureReason { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record CancellationInfo
{
    public DateTime CancelledAt { get; init; }
    public string CancelledBy { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public decimal RefundAmount { get; init; }
    public decimal CancellationFee { get; init; }
    public decimal ProcessingFee { get; init; }
    public CancellationReason CancellationReason { get; init; }
    public bool IsRefundProcessed { get; init; }
    public DateTime? RefundProcessedAt { get; init; }
    public string? RefundTransactionId { get; init; }
}

public record CancellationResult
{
    public decimal RefundAmount { get; init; }
    public decimal CancellationFee { get; init; }
    public decimal ProcessingFee { get; init; }
    public decimal TotalDeductions { get; init; }
    public bool IsEligibleForRefund => RefundAmount > 0;
    public string Summary => $"Refund: {RefundAmount:C}, Fees: {TotalDeductions:C}";
}

// Enums
public enum Gender
{
    Male,
    Female,
    Other,
    PreferNotToSay
}

public enum PassengerType
{
    Adult,
    Child,
    Infant,
    Senior
}

public enum SeatType
{
    Economy,
    PremiumEconomy,
    Business,
    First,
    ExtraLegroom,
    Window,
    Aisle,
    Emergency
}

public enum ExtraServiceType
{
    BaggageAllowance,
    SeatSelection,
    MealUpgrade,
    PriorityBoarding,
    LoungeAccess,
    FastTrackSecurity,
    Insurance,
    SpecialAssistance,
    PetTransport,
    UnaccompaniedMinor,
    WifiAccess,
    Entertainment
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    PayPal,
    BankTransfer,
    DigitalWallet,
    Cryptocurrency,
    GiftCard,
    Points
}



public enum PricingComponentType
{
    BaseFare,
    Tax,
    Fee,
    Extra,
    Discount,
    Upgrade,
    Insurance,
    Service
}

public enum CancellationReason
{
    CustomerRequest,
    FlightCancelled,
    FlightDelayed,
    WeatherConditions,
    MedicalEmergency,
    TravelRestrictions,
    SystemError,
    FraudDetection,
    PaymentFailure,
    Other
}

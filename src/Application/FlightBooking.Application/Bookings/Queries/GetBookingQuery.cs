using FlightBooking.Application.Bookings.Commands;
using FlightBooking.Domain.Bookings;
using MediatR;
using FluentValidation;

namespace FlightBooking.Application.Bookings.Queries;

public record GetBookingQuery : IRequest<BookingDetailDto?>
{
    public Guid? BookingId { get; init; }
    public string? BookingReference { get; init; }
    public bool IncludeHistory { get; init; } = false;
    public bool IncludeEvents { get; init; } = false;
    public string? RequestedBy { get; init; }
}

public record GetBookingHistoryQuery : IRequest<List<BookingHistoryDto>>
{
    public Guid? CustomerId { get; init; }
    public string? GuestId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public List<BookingStatus>? Statuses { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "BookedAt";
    public bool SortDescending { get; init; } = true;
}

public record GetBookingsByFlightQuery : IRequest<List<BookingSummaryDto>>
{
    public Guid FlightId { get; init; }
    public List<BookingStatus>? Statuses { get; init; }
    public bool IncludePassengerCount { get; init; } = true;
    public string? RequestedBy { get; init; }
}

public record ValidateBookingModificationQuery : IRequest<BookingModificationValidationResult>
{
    public Guid BookingId { get; init; }
    public BookingModificationType ModificationType { get; init; }
    public Dictionary<string, object> ModificationData { get; init; } = new();
    public string? RequestedBy { get; init; }
}

public record GetBookingPricingQuery : IRequest<BookingPricingDetailDto>
{
    public Guid BookingId { get; init; }
    public bool IncludeBreakdown { get; init; } = true;
    public bool IncludeTaxDetails { get; init; } = true;
    public string? RequestedBy { get; init; }
}

public record SearchBookingsQuery : IRequest<BookingSearchResult>
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
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "BookedAt";
    public bool SortDescending { get; init; } = true;
    public string? RequestedBy { get; init; }
}

// DTOs
public record BookingDetailDto
{
    public Guid Id { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public BookingStatus Status { get; init; }
    public BookingType Type { get; init; }
    
    // Flight Information
    public FlightInfoDto Flight { get; init; } = new();
    public FareClassInfoDto FareClass { get; init; } = new();
    
    // Customer Information
    public Guid? CustomerId { get; init; }
    public string? GuestId { get; init; }
    public ContactInfoDto ContactInfo { get; init; } = new();
    
    // Passengers and Seats
    public List<PassengerDetailDto> Passengers { get; init; } = new();
    public List<SeatAssignmentDto> SeatAssignments { get; init; } = new();
    
    // Services and Pricing
    public List<BookingExtraDto> Extras { get; init; } = new();
    public BookingPricingDetailDto Pricing { get; init; } = new();
    public string? PromoCode { get; init; }
    public decimal PromoDiscount { get; init; }
    
    // Payment Information
    public PaymentStatus PaymentStatus { get; init; }
    public List<PaymentInfoDto> Payments { get; init; } = new();
    
    // Timestamps
    public DateTime BookedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CheckedInAt { get; init; }
    public DateTime LastModifiedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    
    // Cancellation Information
    public CancellationInfoDto? CancellationInfo { get; init; }
    
    // History and Events (optional)
    public List<BookingModificationDto>? Modifications { get; init; }
    public List<BookingEventDto>? Events { get; init; }
    
    // Policies
    public BookingPoliciesDto Policies { get; init; } = new();
    
    // Metadata
    public string CreatedBy { get; init; } = string.Empty;
    public string? ModifiedBy { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    // Computed Properties
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool CanBeCancelled => Status == BookingStatus.Confirmed && !IsExpired;
    public bool CanBeModified => Status is BookingStatus.Draft or BookingStatus.Confirmed && !IsExpired;
    public bool RequiresPayment => Status == BookingStatus.PaymentPending && !IsExpired;
    public double HoursUntilDeparture => (Flight.DepartureDate - DateTime.UtcNow).TotalHours;
}

public record BookingSummaryDto
{
    public Guid Id { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public BookingStatus Status { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public string Route { get; init; } = string.Empty;
    public int PassengerCount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime BookedAt { get; init; }
    public string ContactEmail { get; init; } = string.Empty;
    public bool IsExpired { get; init; }
}

public record BookingHistoryDto
{
    public Guid Id { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public BookingStatus Status { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public string Route { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime BookedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
}

public record BookingSearchResult
{
    public List<BookingSummaryDto> Bookings { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

public record BookingModificationValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public decimal? EstimatedCost { get; init; }
    public bool RequiresApproval { get; init; }
    public DateTime? Deadline { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record FlightInfoDto
{
    public Guid Id { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public string DepartureAirport { get; init; } = string.Empty;
    public string ArrivalAirport { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string AirlineName { get; init; } = string.Empty;
    public string AircraftType { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
}

public record FareClassInfoDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool IsRefundable { get; init; }
    public bool AllowsChanges { get; init; }
    public List<string> Inclusions { get; init; } = new();
}

public record PassengerDetailDto
{
    public string PassengerReference { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string FullName { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public int Age { get; init; }
    public Gender Gender { get; init; }
    public PassengerType Type { get; init; }
    public PassportInfoDto? Passport { get; init; }
    public string? SpecialRequests { get; init; }
    public string? FrequentFlyerNumber { get; init; }
    public string? KnownTravelerNumber { get; init; }
    public bool RequiresSpecialAssistance { get; init; }
    public List<string> DietaryRestrictions { get; init; } = new();
    public bool IsCheckedIn { get; init; }
    public string? SeatNumber { get; init; }
    public string? BoardingGroup { get; init; }
}

public record SeatAssignmentDto
{
    public string PassengerReference { get; init; } = string.Empty;
    public string SeatNumber { get; init; } = string.Empty;
    public SeatType SeatType { get; init; }
    public decimal SeatFee { get; init; }
    public string? SeatFeatures { get; init; }
    public DateTime AssignedAt { get; init; }
    public string AssignedBy { get; init; } = string.Empty;
}

public record BookingExtraDto
{
    public Guid Id { get; init; }
    public string ServiceCode { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ExtraServiceType Type { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
    public string? PassengerReference { get; init; }
    public bool IsRefundable { get; init; }
    public string? Terms { get; init; }
    public DateTime AddedAt { get; init; }
    public string AddedBy { get; init; } = string.Empty;
}

public record BookingPricingDetailDto
{
    public decimal BaseFare { get; init; }
    public decimal Taxes { get; init; }
    public decimal Fees { get; init; }
    public decimal Extras { get; init; }
    public decimal Discounts { get; init; }
    public decimal UpgradeFees { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "USD";
    public List<PricingComponentDto> Components { get; init; } = new();
    public List<TaxDetailDto> TaxDetails { get; init; } = new();
    public List<FeeDetailDto> FeeDetails { get; init; } = new();
}

public record PricingComponentDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public PricingComponentType Type { get; init; }
    public bool IsRefundable { get; init; }
}

public record TaxDetailDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal Rate { get; init; }
    public string Authority { get; init; } = string.Empty;
    public bool IsRefundable { get; init; }
}

public record FeeDetailDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool IsOptional { get; init; }
    public bool IsRefundable { get; init; }
    public string? WaiverConditions { get; init; }
}

public record CancellationInfoDto
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

public record BookingModificationDto
{
    public Guid Id { get; init; }
    public BookingModificationType ModificationType { get; init; }
    public string Description { get; init; } = string.Empty;
    public string ModifiedBy { get; init; } = string.Empty;
    public DateTime ModifiedAt { get; init; }
    public Dictionary<string, object> PreviousValues { get; init; } = new();
    public Dictionary<string, object> NewValues { get; init; } = new();
    public decimal? CostImpact { get; init; }
    public string? Reason { get; init; }
    public bool RequiresApproval { get; init; }
    public bool IsApproved { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTime? ApprovedAt { get; init; }
}

public record BookingEventDto
{
    public Guid Id { get; init; }
    public BookingEventType EventType { get; init; }
    public string Description { get; init; } = string.Empty;
    public string TriggeredBy { get; init; } = string.Empty;
    public DateTime TriggeredAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public string? ExternalReference { get; init; }
    public bool IsSystemGenerated { get; init; }
}

public record BookingPoliciesDto
{
    public CancellationPolicyDto CancellationPolicy { get; init; } = new();
    public ModificationPolicyDto ModificationPolicy { get; init; } = new();
    public RefundPolicyDto RefundPolicy { get; init; } = new();
    public CheckInPolicyDto CheckInPolicy { get; init; } = new();
}

public record CancellationPolicyDto
{
    public bool IsRefundable { get; init; }
    public decimal ProcessingFee { get; init; }
    public List<CancellationFeeRuleDto> FeeRules { get; init; } = new();
}

public record CancellationFeeRuleDto
{
    public double HoursBeforeDeparture { get; init; }
    public decimal FeePercentage { get; init; }
    public decimal FlatFee { get; init; }
}

public record ModificationPolicyDto
{
    public Dictionary<BookingModificationType, ModificationFeeRuleDto> AllowedModifications { get; init; } = new();
}

public record ModificationFeeRuleDto
{
    public decimal FlatFee { get; init; }
    public decimal FeePercentage { get; init; }
    public double MinHoursBeforeDeparture { get; init; }
    public bool RequiresApproval { get; init; }
}

public record RefundPolicyDto
{
    public bool IsRefundable { get; init; }
    public List<string> Conditions { get; init; } = new();
}

public record CheckInPolicyDto
{
    public int OnlineCheckInHoursBeforeDeparture { get; init; }
    public int CheckInClosesMinutesBeforeDeparture { get; init; }
    public bool RequiresDocumentVerification { get; init; }
    public List<string> RequiredDocuments { get; init; } = new();
}

using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Pricing;

public class FareCalculationRequest
{
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public decimal BaseFare { get; set; }
    public int PassengerCount { get; set; } = 1;
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    public DateTime DepartureDate { get; set; }
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public bool IsRoundTrip { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? PromoCode { get; set; }
    public List<string> PassengerTypes { get; set; } = new(); // Adult, Child, Infant
    public List<ExtraService> RequestedExtras { get; set; } = new();
    public double CurrentLoadFactor { get; set; } // 0.0 to 1.0
    public string? CorporateCode { get; set; }
    public bool IsFlexibleDates { get; set; }
    public string Currency { get; set; } = "USD";
}

public class FareCalculationResult
{
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public FareBreakdown FareBreakdown { get; set; } = new();
    public List<PolicyViolation> PolicyViolations { get; set; } = new();
    public List<AppliedRule> AppliedRules { get; set; } = new();
    public PricingExplanation Explanation { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan CalculationDuration { get; set; }
    public string CalculationId { get; set; } = Guid.NewGuid().ToString();
}

public class FareBreakdown
{
    public decimal BaseFare { get; set; }
    public decimal AdjustedBaseFare { get; set; }
    public List<FareComponent> Components { get; set; } = new();
    public List<Tax> Taxes { get; set; } = new();
    public List<Fee> Fees { get; set; } = new();
    public List<ExtraServiceCharge> Extras { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalExtras { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public string Currency { get; set; } = "USD";

    // Computed properties
    public decimal TotalBeforeTaxes => SubTotal + TotalFees + TotalExtras;
    public decimal EffectiveRate => BaseFare > 0 ? AdjustedBaseFare / BaseFare : 1.0m;
    public decimal SavingsAmount => BaseFare - AdjustedBaseFare + TotalDiscount;
    public bool HasDiscount => TotalDiscount > 0;
}

public class FareComponent
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public FareComponentType Type { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public int PassengerCount { get; set; } = 1;
    public decimal UnitAmount => PassengerCount > 0 ? Amount / PassengerCount : Amount;
}

public class Tax
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Rate { get; set; }
    public TaxType Type { get; set; }
    public string Authority { get; set; } = string.Empty; // Government, Airport, etc.
    public bool IsRefundable { get; set; }
}

public class Fee
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public FeeType Type { get; set; }
    public bool IsOptional { get; set; }
    public bool IsRefundable { get; set; }
    public string? WaiverConditions { get; set; }
}

public class ExtraService
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExtraServiceType Type { get; set; }
    public int Quantity { get; set; } = 1;
    public string? PassengerReference { get; set; }
}

public class ExtraServiceCharge
{
    public ExtraService Service { get; set; } = new();
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsRefundable { get; set; }
    public string? Terms { get; set; }
}

public class PolicyViolation
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PolicySeverity Severity { get; set; }
    public bool IsBlocking { get; set; }
    public string? Resolution { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public class AppliedRule
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Impact { get; set; }
    public string ImpactType { get; set; } = string.Empty; // Multiplier, Addition, Percentage
    public int Priority { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Reason { get; set; }
}

public class PricingExplanation
{
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyFactors { get; set; } = new();
    public List<PricingStep> Steps { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PricingStep
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BeforeAmount { get; set; }
    public decimal AfterAmount { get; set; }
    public decimal Change { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string RuleApplied { get; set; } = string.Empty;
}

// Enums
public enum FareComponentType
{
    BaseFare,
    WeekendSurcharge,
    SeasonalAdjustment,
    DemandSurcharge,
    PromotionalDiscount,
    CorporateDiscount,
    AdvancePurchaseDiscount,
    LastMinuteSurcharge,
    RouteSpecificAdjustment,
    LoadFactorAdjustment
}

public enum TaxType
{
    GovernmentTax,
    AirportTax,
    SecurityTax,
    FuelSurcharge,
    ServiceTax,
    InternationalTax,
    DomesticTax,
    TransitTax
}

public enum FeeType
{
    BookingFee,
    ServiceFee,
    ProcessingFee,
    CancellationFee,
    ChangeFee,
    SeatSelectionFee,
    PriorityBoardingFee,
    FastTrackFee
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
    UnaccompaniedMinor
}

public enum PolicySeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public class PricingRule : BaseEntity
{
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PricingRuleType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Conditions { get; set; } = string.Empty; // JSON
    public string Parameters { get; set; } = string.Empty; // JSON
    public List<string> ApplicableRoutes { get; set; } = new();
    public List<string> ApplicableFareClasses { get; set; } = new();
    public List<string> ExcludedDates { get; set; } = new();
    public bool CanCombineWithOthers { get; set; } = true;
    public string? ConflictResolution { get; set; }
}

public enum PricingRuleType
{
    WeekendSurcharge,
    SeasonalMultiplier,
    DemandBased,
    PromotionalDiscount,
    AdvancePurchase,
    LastMinute,
    RouteSpecific,
    LoadFactorBased,
    CorporateDiscount,
    GroupDiscount
}

using FlightBooking.Contracts.Common;

namespace FlightBooking.Contracts.Pricing;

public class FareCalculationRequestDto
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
    public List<string> PassengerTypes { get; set; } = new();
    public List<ExtraServiceDto> RequestedExtras { get; set; } = new();
    public double CurrentLoadFactor { get; set; }
    public string? CorporateCode { get; set; }
    public bool IsFlexibleDates { get; set; }
    public string Currency { get; set; } = "USD";
}

public class FareCalculationResponseDto : BaseResponse
{
    public FareBreakdownDto FareBreakdown { get; set; } = new();
    public List<PolicyViolationDto> PolicyViolations { get; set; } = new();
    public List<AppliedRuleDto> AppliedRules { get; set; } = new();
    public PricingExplanationDto Explanation { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public TimeSpan CalculationDuration { get; set; }
    public string CalculationId { get; set; } = string.Empty;
}

public class FareBreakdownDto
{
    public decimal BaseFare { get; set; }
    public decimal AdjustedBaseFare { get; set; }
    public List<FareComponentDto> Components { get; set; } = new();
    public List<TaxDto> Taxes { get; set; } = new();
    public List<FeeDto> Fees { get; set; } = new();
    public List<ExtraServiceChargeDto> Extras { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalExtras { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal TotalBeforeTaxes { get; set; }
    public decimal EffectiveRate { get; set; }
    public decimal SavingsAmount { get; set; }
    public bool HasDiscount { get; set; }
}

public class FareComponentDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public int PassengerCount { get; set; } = 1;
    public decimal UnitAmount { get; set; }
}

public class TaxDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Rate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
}

public class FeeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public bool IsRefundable { get; set; }
    public string? WaiverConditions { get; set; }
}

public class ExtraServiceDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? PassengerReference { get; set; }
}

public class ExtraServiceChargeDto
{
    public ExtraServiceDto Service { get; set; } = new();
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsRefundable { get; set; }
    public string? Terms { get; set; }
}

public class PolicyViolationDto
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsBlocking { get; set; }
    public string? Resolution { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public class AppliedRuleDto
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Impact { get; set; }
    public string ImpactType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime AppliedAt { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Reason { get; set; }
}

public class PricingExplanationDto
{
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyFactors { get; set; } = new();
    public List<PricingStepDto> Steps { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PricingStepDto
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BeforeAmount { get; set; }
    public decimal AfterAmount { get; set; }
    public decimal Change { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string RuleApplied { get; set; } = string.Empty;
}

public class PolicyValidationRequestDto
{
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    public DateTime DepartureDate { get; set; }
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public bool IsRoundTrip { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int PassengerCount { get; set; } = 1;
    public string? CorporateCode { get; set; }
}

public class PolicyValidationResponseDto : BaseResponse
{
    public bool IsValid { get; set; } = true;
    public List<PolicyViolationDto> Violations { get; set; } = new();
    public List<PolicyWarningDto> Warnings { get; set; } = new();
    public List<PolicyRecommendationDto> Recommendations { get; set; } = new();
    public bool RequiresApproval { get; set; }
    public string? ApprovalReason { get; set; }
    public List<string> RequiredDocuments { get; set; } = new();
}

public class PolicyWarningDto
{
    public string PolicyId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
    public bool CanProceed { get; set; } = true;
}

public class PolicyRecommendationDto
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? PotentialSavings { get; set; }
    public string? AlternativeAction { get; set; }
    public int Priority { get; set; }
}

public class TaxAndFeeBreakdownRequestDto
{
    public string Route { get; set; } = string.Empty;
    public string FareClass { get; set; } = string.Empty;
    public decimal BaseFare { get; set; }
}

public class TaxAndFeeBreakdownResponseDto : BaseResponse
{
    public List<TaxDto> Taxes { get; set; } = new();
    public List<FeeDto> Fees { get; set; } = new();
    public decimal TotalTaxes { get; set; }
    public decimal TotalFees { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CalculatedAt { get; set; }
    public List<string> TaxExemptions { get; set; } = new();
    public List<string> FeeWaivers { get; set; } = new();
}

public class ExtraServicesCalculationRequestDto
{
    public List<ExtraServiceDto> Services { get; set; } = new();
    public FareCalculationRequestDto FareRequest { get; set; } = new();
}

public class ExtraServicesCalculationResponseDto : BaseResponse
{
    public List<ExtraServiceChargeDto> ServiceCharges { get; set; } = new();
    public decimal TotalCharges { get; set; }
    public List<string> UnavailableServices { get; set; } = new();
    public List<ServiceRecommendationDto> Recommendations { get; set; } = new();
    public Dictionary<string, decimal> BundleDiscounts { get; set; } = new();
    public string Currency { get; set; } = "USD";
}

public class ServiceRecommendationDto
{
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal PotentialSavings { get; set; }
    public int Priority { get; set; }
}

public class PromoCodeValidationRequestDto
{
    public string PromoCode { get; set; } = string.Empty;
    public FareCalculationRequestDto FareRequest { get; set; } = new();
}

public class PromoCodeValidationResponseDto : BaseResponse
{
    public bool IsValid { get; set; }
    public decimal EstimatedDiscount { get; set; }
    public List<string> Terms { get; set; } = new();
    public DateTime? ExpiryDate { get; set; }
    public List<string> Restrictions { get; set; } = new();
}

public class AvailablePromotionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedSavings { get; set; }
    public DateTime ExpiryDate { get; set; }
    public List<string> Terms { get; set; } = new();
    public bool RequiresMinimumPurchase { get; set; }
    public decimal? MinimumPurchaseAmount { get; set; }
}

public class PricingEducationResponseDto : BaseResponse
{
    public string Summary { get; set; } = string.Empty;
    public List<PricingFactorDto> Factors { get; set; } = new();
    public List<SavingsTipDto> SavingsTips { get; set; } = new();
    public List<PricingComparisonDto> Comparisons { get; set; } = new();
    public Dictionary<string, string> Glossary { get; set; } = new();
}

public class PricingFactorDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Impact { get; set; }
    public string ImpactType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class SavingsTipDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialSavings { get; set; }
    public string ActionRequired { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class PricingComparisonDto
{
    public string Scenario { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Difference { get; set; }
    public string Description { get; set; } = string.Empty;
}

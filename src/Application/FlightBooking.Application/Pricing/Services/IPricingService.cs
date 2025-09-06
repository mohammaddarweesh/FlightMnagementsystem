using FlightBooking.Domain.Pricing;
using FlightBooking.Application.Pricing.Queries;

namespace FlightBooking.Application.Pricing.Services;

public interface IPricingService
{
    /// <summary>
    /// Calculates comprehensive fare pricing with all applicable rules and policies
    /// </summary>
    Task<FareCalculationResult> CalculateFareAsync(FareCalculationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates pricing policies without calculating final fare
    /// </summary>
    Task<PolicyValidationResult> ValidatePoliciesAsync(FareCalculationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed breakdown of taxes and fees for a route
    /// </summary>
    Task<TaxAndFeeBreakdown> GetTaxAndFeeBreakdownAsync(string route, string fareClass, decimal baseFare, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates pricing for extra services (baggage, seats, etc.)
    /// </summary>
    Task<ExtraServicesCalculation> CalculateExtraServicesAsync(List<ExtraService> services, FareCalculationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available promotional codes for a route and date
    /// </summary>
    Task<List<AvailablePromotion>> GetAvailablePromotionsAsync(string route, DateTime departureDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a promotional code
    /// </summary>
    Task<PromoCodeValidationResult> ValidatePromoCodeAsync(string promoCode, FareCalculationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pricing explanation for educational purposes
    /// </summary>
    Task<PricingEducation> GetPricingEducationAsync(FareCalculationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates pricing for different scenarios (what-if analysis)
    /// </summary>
    Task<List<PricingScenario>> SimulatePricingScenariosAsync(FareCalculationRequest baseRequest, List<ScenarioParameter> scenarios, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates pricing for a specific flight and passenger count
    /// </summary>
    Task<FareCalculationResult> CalculatePricingAsync(CalculatePricingQuery query, CancellationToken cancellationToken = default);
}

public class TaxAndFeeBreakdown
{
    public List<Tax> Taxes { get; set; } = new();
    public List<Fee> Fees { get; set; } = new();
    public decimal TotalTaxes { get; set; }
    public decimal TotalFees { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public List<string> TaxExemptions { get; set; } = new();
    public List<string> FeeWaivers { get; set; } = new();
}

public class ExtraServicesCalculation
{
    public List<ExtraServiceCharge> ServiceCharges { get; set; } = new();
    public decimal TotalCharges { get; set; }
    public List<string> UnavailableServices { get; set; } = new();
    public List<ServiceRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, decimal> BundleDiscounts { get; set; } = new();
    public string Currency { get; set; } = "USD";
}

public class ServiceRecommendation
{
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal PotentialSavings { get; set; }
    public int Priority { get; set; }
}

public class AvailablePromotion
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

public class PromoCodeValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal EstimatedDiscount { get; set; }
    public List<string> Terms { get; set; } = new();
    public DateTime? ExpiryDate { get; set; }
    public List<string> Restrictions { get; set; } = new();
}

public class PricingEducation
{
    public string Summary { get; set; } = string.Empty;
    public List<PricingFactor> Factors { get; set; } = new();
    public List<SavingsTip> SavingsTips { get; set; } = new();
    public List<PricingComparison> Comparisons { get; set; } = new();
    public Dictionary<string, string> Glossary { get; set; } = new();
}

public class PricingFactor
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Impact { get; set; }
    public string ImpactType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class SavingsTip
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialSavings { get; set; }
    public string ActionRequired { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class PricingComparison
{
    public string Scenario { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Difference { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PricingScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FareCalculationRequest ModifiedRequest { get; set; } = new();
    public FareCalculationResult Result { get; set; } = new();
    public decimal PriceDifference { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class ScenarioParameter
{
    public string ParameterName { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

public interface ITaxCalculationService
{
    Task<List<Tax>> CalculateTaxesAsync(string route, string fareClass, decimal baseFare, CancellationToken cancellationToken = default);
    Task<List<Fee>> CalculateFeesAsync(string route, string fareClass, decimal baseFare, CancellationToken cancellationToken = default);
}

public interface IExtraServicesService
{
    Task<List<ExtraServiceCharge>> CalculateServiceChargesAsync(List<ExtraService> services, FareCalculationRequest request, CancellationToken cancellationToken = default);
    Task<List<ExtraService>> GetAvailableServicesAsync(Guid flightId, Guid fareClassId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, decimal>> GetBundleDiscountsAsync(List<ExtraService> services, CancellationToken cancellationToken = default);
}

public interface IPromotionService
{
    Task<List<AvailablePromotion>> GetAvailablePromotionsAsync(string route, DateTime departureDate, CancellationToken cancellationToken = default);
    Task<PromoCodeValidationResult> ValidatePromoCodeAsync(string promoCode, FareCalculationRequest request, CancellationToken cancellationToken = default);
    Task<decimal> CalculatePromotionDiscountAsync(string promoCode, decimal baseFare, FareCalculationRequest request, CancellationToken cancellationToken = default);
}

public interface IPricingAnalyticsService
{
    Task<PricingEducation> GeneratePricingEducationAsync(FareCalculationRequest request, FareCalculationResult result, CancellationToken cancellationToken = default);
    Task<List<SavingsTip>> GetSavingsTipsAsync(FareCalculationRequest request, CancellationToken cancellationToken = default);
    Task<List<PricingComparison>> GetPricingComparisonsAsync(FareCalculationRequest request, CancellationToken cancellationToken = default);
    Task<List<PricingScenario>> SimulateScenariosAsync(FareCalculationRequest baseRequest, List<ScenarioParameter> scenarios, CancellationToken cancellationToken = default);
}

public interface IPricingConfigurationService
{
    Task<List<PricingRule>> GetActivePricingRulesAsync(CancellationToken cancellationToken = default);
    Task<List<PricingPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);
    Task<PricingConfiguration> GetPricingConfigurationAsync(string route, string fareClass, CancellationToken cancellationToken = default);
    Task UpdatePricingRuleAsync(PricingRule rule, CancellationToken cancellationToken = default);
    Task UpdatePricingPolicyAsync(PricingPolicy policy, CancellationToken cancellationToken = default);
}

public class PricingConfiguration
{
    public List<PricingRule> Rules { get; set; } = new();
    public List<PricingPolicy> Policies { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public string Version { get; set; } = string.Empty;
}

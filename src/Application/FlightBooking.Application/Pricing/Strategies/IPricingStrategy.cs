using FlightBooking.Domain.Pricing;

namespace FlightBooking.Application.Pricing.Strategies;

public interface IPricingStrategy
{
    string StrategyName { get; }
    PricingRuleType RuleType { get; }
    int Priority { get; }
    bool CanApply(FareCalculationRequest request, PricingContext context);
    Task<PricingResult> ApplyAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default);
}

public interface IPolicyValidator
{
    string ValidatorName { get; }
    PolicyType PolicyType { get; }
    Task<PolicyValidationResult> ValidateAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default);
}

public class PricingContext
{
    public decimal CurrentFare { get; set; }
    public decimal OriginalBaseFare { get; set; }
    public List<AppliedRule> AppliedRules { get; set; } = new();
    public List<PolicyViolation> PolicyViolations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CalculationStartTime { get; set; } = DateTime.UtcNow;
    public List<PricingStep> Steps { get; set; } = new();
    public bool IsTestMode { get; set; } = false;
    public string? OverrideReason { get; set; }
    public List<string> ExcludedRules { get; set; } = new();
    public List<string> ForceApplyRules { get; set; } = new();

    public void AddStep(string description, decimal beforeAmount, decimal afterAmount, string ruleApplied)
    {
        Steps.Add(new PricingStep
        {
            Order = Steps.Count + 1,
            Description = description,
            BeforeAmount = beforeAmount,
            AfterAmount = afterAmount,
            Change = afterAmount - beforeAmount,
            ChangeType = afterAmount > beforeAmount ? "Increase" : afterAmount < beforeAmount ? "Decrease" : "No Change",
            RuleApplied = ruleApplied
        });
    }

    public void AddRule(AppliedRule rule)
    {
        AppliedRules.Add(rule);
        Metadata[$"rule_{rule.RuleId}_applied"] = true;
        Metadata[$"rule_{rule.RuleId}_impact"] = rule.Impact;
    }

    public bool HasRuleBeenApplied(string ruleId)
    {
        return AppliedRules.Any(r => r.RuleId == ruleId);
    }

    public decimal GetTotalAdjustment()
    {
        return CurrentFare - OriginalBaseFare;
    }

    public decimal GetTotalMultiplier()
    {
        return OriginalBaseFare > 0 ? CurrentFare / OriginalBaseFare : 1.0m;
    }
}

public class PricingResult
{
    public bool Success { get; set; } = true;
    public decimal AdjustedFare { get; set; }
    public decimal Adjustment { get; set; }
    public string? ErrorMessage { get; set; }
    public AppliedRule? AppliedRule { get; set; }
    public List<FareComponent> Components { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? Explanation { get; set; }
}

public abstract class BasePricingStrategy : IPricingStrategy
{
    public abstract string StrategyName { get; }
    public abstract PricingRuleType RuleType { get; }
    public abstract int Priority { get; }

    public virtual bool CanApply(FareCalculationRequest request, PricingContext context)
    {
        // Default implementation - can be overridden
        return !context.ExcludedRules.Contains(StrategyName) &&
               !context.HasRuleBeenApplied(StrategyName);
    }

    public abstract Task<PricingResult> ApplyAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default);

    protected AppliedRule CreateAppliedRule(string ruleId, string ruleName, decimal impact, string impactType, string? reason = null, Dictionary<string, object>? parameters = null)
    {
        return new AppliedRule
        {
            RuleId = ruleId,
            RuleName = ruleName,
            RuleType = RuleType.ToString(),
            Description = $"{StrategyName} applied",
            Impact = impact,
            ImpactType = impactType,
            Priority = Priority,
            Reason = reason,
            Parameters = parameters ?? new Dictionary<string, object>()
        };
    }

    protected FareComponent CreateFareComponent(string name, decimal amount, string? description = null, bool isOptional = false)
    {
        return new FareComponent
        {
            Name = name,
            Description = description ?? name,
            Amount = amount,
            Type = GetFareComponentType(),
            RuleId = StrategyName,
            IsOptional = isOptional
        };
    }

    protected virtual FareComponentType GetFareComponentType()
    {
        return RuleType switch
        {
            PricingRuleType.WeekendSurcharge => FareComponentType.WeekendSurcharge,
            PricingRuleType.SeasonalMultiplier => FareComponentType.SeasonalAdjustment,
            PricingRuleType.DemandBased => FareComponentType.DemandSurcharge,
            PricingRuleType.PromotionalDiscount => FareComponentType.PromotionalDiscount,
            PricingRuleType.AdvancePurchase => FareComponentType.AdvancePurchaseDiscount,
            PricingRuleType.LastMinute => FareComponentType.LastMinuteSurcharge,
            PricingRuleType.RouteSpecific => FareComponentType.RouteSpecificAdjustment,
            PricingRuleType.LoadFactorBased => FareComponentType.LoadFactorAdjustment,
            PricingRuleType.CorporateDiscount => FareComponentType.CorporateDiscount,
            _ => FareComponentType.BaseFare
        };
    }

    protected bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    protected bool IsHoliday(DateTime date)
    {
        // Simplified holiday detection - can be enhanced with proper holiday calendar
        var holidays = new[]
        {
            new DateTime(date.Year, 1, 1),   // New Year
            new DateTime(date.Year, 7, 4),   // Independence Day
            new DateTime(date.Year, 12, 25), // Christmas
        };

        return holidays.Any(h => h.Date == date.Date);
    }

    protected int GetDaysUntilDeparture(DateTime bookingDate, DateTime departureDate)
    {
        return (int)(departureDate.Date - bookingDate.Date).TotalDays;
    }

    protected bool IsInDateRange(DateTime date, List<DateRange> ranges)
    {
        return ranges.Any(range => range.Contains(date));
    }

    protected string FormatCurrency(decimal amount, string currency = "USD")
    {
        return currency switch
        {
            "USD" => $"${amount:F2}",
            "EUR" => $"€{amount:F2}",
            "GBP" => $"£{amount:F2}",
            _ => $"{amount:F2} {currency}"
        };
    }
}

public abstract class BasePolicyValidator : IPolicyValidator
{
    public abstract string ValidatorName { get; }
    public abstract PolicyType PolicyType { get; }

    public abstract Task<PolicyValidationResult> ValidateAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default);

    protected PolicyViolation CreateViolation(string policyId, string policyName, string violationType, string description, PolicySeverity severity, bool isBlocking = false, string? resolution = null)
    {
        return new PolicyViolation
        {
            PolicyId = policyId,
            PolicyName = policyName,
            ViolationType = violationType,
            Description = description,
            Severity = severity,
            IsBlocking = isBlocking,
            Resolution = resolution,
            Context = new Dictionary<string, object>()
        };
    }

    protected PolicyWarning CreateWarning(string policyId, string message, string? suggestion = null, bool canProceed = true)
    {
        return new PolicyWarning
        {
            PolicyId = policyId,
            Message = message,
            Suggestion = suggestion,
            CanProceed = canProceed
        };
    }

    protected PolicyRecommendation CreateRecommendation(string type, string message, decimal? potentialSavings = null, string? alternativeAction = null, int priority = 1)
    {
        return new PolicyRecommendation
        {
            Type = type,
            Message = message,
            PotentialSavings = potentialSavings,
            AlternativeAction = alternativeAction,
            Priority = priority
        };
    }
}

using FlightBooking.Domain.Pricing;

namespace FlightBooking.Application.Pricing.Strategies;

public class WeekendSurchargeStrategy : BasePricingStrategy
{
    public override string StrategyName => "WeekendSurcharge";
    public override PricingRuleType RuleType => PricingRuleType.WeekendSurcharge;
    public override int Priority => 100;

    private readonly WeekendSurchargeConfig _config;

    public WeekendSurchargeStrategy(WeekendSurchargeConfig config)
    {
        _config = config;
    }

    public override bool CanApply(FareCalculationRequest request, PricingContext context)
    {
        if (!base.CanApply(request, context))
            return false;

        // Apply if departure is on weekend
        return IsWeekend(request.DepartureDate) || 
               (request.IsRoundTrip && request.ReturnDate.HasValue && IsWeekend(request.ReturnDate.Value));
    }

    public override Task<PricingResult> ApplyAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default)
    {
        var result = new PricingResult();
        
        try
        {
            var surchargeAmount = CalculateSurcharge(request, context);
            var adjustedFare = context.CurrentFare + surchargeAmount;

            var appliedRule = CreateAppliedRule(
                ruleId: "WEEKEND_SURCHARGE",
                ruleName: "Weekend Travel Surcharge",
                impact: surchargeAmount,
                impactType: "Addition",
                reason: GetSurchargeReason(request),
                parameters: new Dictionary<string, object>
                {
                    ["departure_day"] = request.DepartureDate.DayOfWeek.ToString(),
                    ["return_day"] = request.ReturnDate?.DayOfWeek.ToString() ?? "N/A",
                    ["surcharge_percentage"] = _config.SurchargePercentage,
                    ["flat_surcharge"] = _config.FlatSurcharge,
                    ["is_holiday"] = IsHoliday(request.DepartureDate)
                });

            var component = CreateFareComponent(
                name: "Weekend Surcharge",
                amount: surchargeAmount,
                description: $"Weekend travel surcharge for {request.DepartureDate.DayOfWeek}");

            result.Success = true;
            result.AdjustedFare = adjustedFare;
            result.Adjustment = surchargeAmount;
            result.AppliedRule = appliedRule;
            result.Components.Add(component);
            result.Explanation = GenerateExplanation(request, surchargeAmount);

            context.AddStep(
                description: $"Applied weekend surcharge ({_config.SurchargePercentage:P1})",
                beforeAmount: context.CurrentFare,
                afterAmount: adjustedFare,
                ruleApplied: StrategyName);

            context.CurrentFare = adjustedFare;
            context.AddRule(appliedRule);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error applying weekend surcharge: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    private decimal CalculateSurcharge(FareCalculationRequest request, PricingContext context)
    {
        var baseSurcharge = 0m;

        // Calculate percentage-based surcharge
        if (_config.SurchargePercentage > 0)
        {
            baseSurcharge += context.CurrentFare * (_config.SurchargePercentage / 100m);
        }

        // Add flat surcharge
        if (_config.FlatSurcharge > 0)
        {
            baseSurcharge += _config.FlatSurcharge;
        }

        // Apply multipliers for special cases
        var multiplier = 1.0m;

        // Holiday multiplier
        if (IsHoliday(request.DepartureDate) && _config.HolidayMultiplier > 1)
        {
            multiplier *= _config.HolidayMultiplier;
        }

        // Peak season multiplier
        if (IsPeakSeason(request.DepartureDate) && _config.PeakSeasonMultiplier > 1)
        {
            multiplier *= _config.PeakSeasonMultiplier;
        }

        // Round trip multiplier (if both departure and return are weekends)
        if (request.IsRoundTrip && request.ReturnDate.HasValue && 
            IsWeekend(request.DepartureDate) && IsWeekend(request.ReturnDate.Value))
        {
            multiplier *= _config.RoundTripWeekendMultiplier;
        }

        return Math.Round(baseSurcharge * multiplier, 2);
    }

    private bool IsPeakSeason(DateTime date)
    {
        // Summer peak season (June-August)
        if (date.Month >= 6 && date.Month <= 8)
            return true;

        // Winter holidays (December-January)
        if (date.Month == 12 || date.Month == 1)
            return true;

        return false;
    }

    private string GetSurchargeReason(FareCalculationRequest request)
    {
        var reasons = new List<string>();

        if (IsWeekend(request.DepartureDate))
            reasons.Add($"departure on {request.DepartureDate.DayOfWeek}");

        if (request.IsRoundTrip && request.ReturnDate.HasValue && IsWeekend(request.ReturnDate.Value))
            reasons.Add($"return on {request.ReturnDate.Value.DayOfWeek}");

        if (IsHoliday(request.DepartureDate))
            reasons.Add("holiday travel");

        if (IsPeakSeason(request.DepartureDate))
            reasons.Add("peak season");

        return $"Weekend surcharge applied due to {string.Join(", ", reasons)}";
    }

    private string GenerateExplanation(FareCalculationRequest request, decimal surchargeAmount)
    {
        var explanation = $"A weekend surcharge of {FormatCurrency(surchargeAmount)} has been applied because ";
        
        if (IsWeekend(request.DepartureDate))
        {
            explanation += $"your departure is on {request.DepartureDate.DayOfWeek}";
        }

        if (request.IsRoundTrip && request.ReturnDate.HasValue && IsWeekend(request.ReturnDate.Value))
        {
            explanation += IsWeekend(request.DepartureDate) ? 
                $" and your return is on {request.ReturnDate.Value.DayOfWeek}" : 
                $"your return is on {request.ReturnDate.Value.DayOfWeek}";
        }

        explanation += ". Weekend travel typically has higher demand and limited availability.";

        if (IsHoliday(request.DepartureDate))
        {
            explanation += " Additional holiday surcharge has been applied.";
        }

        return explanation;
    }
}

public class WeekendSurchargeConfig
{
    public decimal SurchargePercentage { get; set; } = 15.0m; // 15% surcharge
    public decimal FlatSurcharge { get; set; } = 0m; // Optional flat amount
    public decimal HolidayMultiplier { get; set; } = 1.5m; // 50% more on holidays
    public decimal PeakSeasonMultiplier { get; set; } = 1.2m; // 20% more in peak season
    public decimal RoundTripWeekendMultiplier { get; set; } = 1.1m; // 10% more for round trip weekends
    public bool ApplyToReturnDate { get; set; } = true;
    public List<string> ExemptRoutes { get; set; } = new();
    public List<string> ExemptFareClasses { get; set; } = new();
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class SeasonalMultiplierStrategy : BasePricingStrategy
{
    public override string StrategyName => "SeasonalMultiplier";
    public override PricingRuleType RuleType => PricingRuleType.SeasonalMultiplier;
    public override int Priority => 90;

    private readonly SeasonalMultiplierConfig _config;

    public SeasonalMultiplierStrategy(SeasonalMultiplierConfig config)
    {
        _config = config;
    }

    public override bool CanApply(FareCalculationRequest request, PricingContext context)
    {
        if (!base.CanApply(request, context))
            return false;

        return GetSeasonalMultiplier(request.DepartureDate) != 1.0m;
    }

    public override Task<PricingResult> ApplyAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default)
    {
        var result = new PricingResult();
        
        try
        {
            var multiplier = GetSeasonalMultiplier(request.DepartureDate);
            var adjustedFare = context.CurrentFare * multiplier;
            var adjustment = adjustedFare - context.CurrentFare;

            var season = GetSeason(request.DepartureDate);
            var appliedRule = CreateAppliedRule(
                ruleId: $"SEASONAL_{season.ToUpper()}",
                ruleName: $"{season} Seasonal Pricing",
                impact: adjustment,
                impactType: "Multiplier",
                reason: $"Seasonal adjustment for {season} travel",
                parameters: new Dictionary<string, object>
                {
                    ["season"] = season,
                    ["multiplier"] = multiplier,
                    ["departure_month"] = request.DepartureDate.Month
                });

            var component = CreateFareComponent(
                name: $"{season} Seasonal Adjustment",
                amount: adjustment,
                description: $"Seasonal pricing adjustment for {season} travel");

            result.Success = true;
            result.AdjustedFare = adjustedFare;
            result.Adjustment = adjustment;
            result.AppliedRule = appliedRule;
            result.Components.Add(component);
            result.Explanation = $"Seasonal {(adjustment > 0 ? "surcharge" : "discount")} of {FormatCurrency(Math.Abs(adjustment))} applied for {season} travel.";

            context.AddStep(
                description: $"Applied {season} seasonal multiplier ({multiplier:P1})",
                beforeAmount: context.CurrentFare,
                afterAmount: adjustedFare,
                ruleApplied: StrategyName);

            context.CurrentFare = adjustedFare;
            context.AddRule(appliedRule);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error applying seasonal multiplier: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    private decimal GetSeasonalMultiplier(DateTime date)
    {
        return date.Month switch
        {
            12 or 1 => _config.WinterMultiplier,      // Winter holidays
            3 or 4 => _config.SpringMultiplier,       // Spring break
            6 or 7 or 8 => _config.SummerMultiplier,  // Summer vacation
            11 => _config.ThanksgivingMultiplier,     // Thanksgiving
            _ => 1.0m
        };
    }

    private string GetSeason(DateTime date)
    {
        return date.Month switch
        {
            12 or 1 => "Winter Holiday",
            3 or 4 => "Spring",
            6 or 7 or 8 => "Summer",
            11 => "Thanksgiving",
            _ => "Regular"
        };
    }
}

public class SeasonalMultiplierConfig
{
    public decimal WinterMultiplier { get; set; } = 1.3m; // 30% increase
    public decimal SpringMultiplier { get; set; } = 1.15m; // 15% increase
    public decimal SummerMultiplier { get; set; } = 1.25m; // 25% increase
    public decimal ThanksgivingMultiplier { get; set; } = 1.4m; // 40% increase
    public List<string> ExemptRoutes { get; set; } = new();
    public List<string> ExemptFareClasses { get; set; } = new();
}

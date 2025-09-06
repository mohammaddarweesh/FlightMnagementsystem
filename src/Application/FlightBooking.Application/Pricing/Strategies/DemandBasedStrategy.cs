using FlightBooking.Domain.Pricing;

namespace FlightBooking.Application.Pricing.Strategies;

public class DemandBasedStrategy : BasePricingStrategy
{
    public override string StrategyName => "DemandBased";
    public override PricingRuleType RuleType => PricingRuleType.DemandBased;
    public override int Priority => 80;

    private readonly DemandBasedConfig _config;

    public DemandBasedStrategy(DemandBasedConfig config)
    {
        _config = config;
    }

    public override bool CanApply(FareCalculationRequest request, PricingContext context)
    {
        if (!base.CanApply(request, context))
            return false;

        // Apply if load factor is above minimum threshold
        return request.CurrentLoadFactor >= _config.MinimumLoadFactorThreshold;
    }

    public override Task<PricingResult> ApplyAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default)
    {
        var result = new PricingResult();
        
        try
        {
            var loadFactorTier = GetLoadFactorTier(request.CurrentLoadFactor);
            var multiplier = GetDemandMultiplier(loadFactorTier);
            var adjustedFare = context.CurrentFare * multiplier;
            var adjustment = adjustedFare - context.CurrentFare;

            var appliedRule = CreateAppliedRule(
                ruleId: $"DEMAND_{loadFactorTier.Name.ToUpper()}",
                ruleName: $"Demand-Based Pricing - {loadFactorTier.Name}",
                impact: adjustment,
                impactType: "Multiplier",
                reason: $"High demand pricing due to {request.CurrentLoadFactor:P1} load factor",
                parameters: new Dictionary<string, object>
                {
                    ["load_factor"] = request.CurrentLoadFactor,
                    ["tier"] = loadFactorTier.Name,
                    ["multiplier"] = multiplier,
                    ["days_until_departure"] = GetDaysUntilDeparture(request.BookingDate, request.DepartureDate)
                });

            var component = CreateFareComponent(
                name: $"Demand Surcharge - {loadFactorTier.Name}",
                amount: adjustment,
                description: $"Demand-based pricing adjustment due to {loadFactorTier.Name.ToLower()} demand ({request.CurrentLoadFactor:P1} capacity)");

            result.Success = true;
            result.AdjustedFare = adjustedFare;
            result.Adjustment = adjustment;
            result.AppliedRule = appliedRule;
            result.Components.Add(component);
            result.Explanation = GenerateExplanation(request, loadFactorTier, adjustment);

            context.AddStep(
                description: $"Applied {loadFactorTier.Name.ToLower()} demand multiplier ({multiplier:P1})",
                beforeAmount: context.CurrentFare,
                afterAmount: adjustedFare,
                ruleApplied: StrategyName);

            context.CurrentFare = adjustedFare;
            context.AddRule(appliedRule);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error applying demand-based pricing: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    private LoadFactorTier GetLoadFactorTier(double loadFactor)
    {
        return _config.LoadFactorTiers
            .OrderByDescending(t => t.MinLoadFactor)
            .FirstOrDefault(t => loadFactor >= t.MinLoadFactor) 
            ?? _config.LoadFactorTiers.First();
    }

    private decimal GetDemandMultiplier(LoadFactorTier tier)
    {
        return tier.Multiplier;
    }

    private string GenerateExplanation(FareCalculationRequest request, LoadFactorTier tier, decimal adjustment)
    {
        var explanation = $"A demand surcharge of {FormatCurrency(adjustment)} has been applied because this flight is experiencing {tier.Name.ToLower()} demand ";
        explanation += $"with {request.CurrentLoadFactor:P1} of seats already booked. ";
        
        var daysUntilDeparture = GetDaysUntilDeparture(request.BookingDate, request.DepartureDate);
        if (daysUntilDeparture <= 7)
        {
            explanation += "Last-minute bookings on high-demand flights typically carry premium pricing.";
        }
        else
        {
            explanation += "Popular flights with limited availability are priced at a premium.";
        }

        return explanation;
    }
}

public class DemandBasedConfig
{
    public double MinimumLoadFactorThreshold { get; set; } = 0.6; // 60%
    public List<LoadFactorTier> LoadFactorTiers { get; set; } = new()
    {
        new() { Name = "High", MinLoadFactor = 0.85, Multiplier = 1.3m },
        new() { Name = "Very High", MinLoadFactor = 0.95, Multiplier = 1.5m },
        new() { Name = "Moderate", MinLoadFactor = 0.70, Multiplier = 1.15m },
        new() { Name = "Standard", MinLoadFactor = 0.60, Multiplier = 1.05m }
    };
}

public class LoadFactorTier
{
    public string Name { get; set; } = string.Empty;
    public double MinLoadFactor { get; set; }
    public decimal Multiplier { get; set; }
    public string? Description { get; set; }
}

public class PromotionalDiscountStrategy : BasePricingStrategy
{
    public override string StrategyName => "PromotionalDiscount";
    public override PricingRuleType RuleType => PricingRuleType.PromotionalDiscount;
    public override int Priority => 70;

    private readonly PromotionalDiscountConfig _config;

    public PromotionalDiscountStrategy(PromotionalDiscountConfig config)
    {
        _config = config;
    }

    public override bool CanApply(FareCalculationRequest request, PricingContext context)
    {
        if (!base.CanApply(request, context))
            return false;

        if (string.IsNullOrEmpty(request.PromoCode))
            return false;

        var promo = GetValidPromotion(request.PromoCode, request);
        return promo != null;
    }

    public override Task<PricingResult> ApplyAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default)
    {
        var result = new PricingResult();
        
        try
        {
            var promo = GetValidPromotion(request.PromoCode!, request);
            if (promo == null)
            {
                result.Success = false;
                result.ErrorMessage = "Invalid or expired promotional code";
                return Task.FromResult(result);
            }

            var discountAmount = CalculateDiscount(promo, context.CurrentFare, request);
            var adjustedFare = context.CurrentFare - discountAmount;

            var appliedRule = CreateAppliedRule(
                ruleId: $"PROMO_{promo.Code}",
                ruleName: $"Promotional Discount - {promo.Name}",
                impact: -discountAmount,
                impactType: promo.Type == PromotionType.Percentage ? "Percentage" : "Flat",
                reason: $"Promotional discount applied: {promo.Description}",
                parameters: new Dictionary<string, object>
                {
                    ["promo_code"] = promo.Code,
                    ["promo_type"] = promo.Type.ToString(),
                    ["discount_value"] = promo.Value,
                    ["max_discount"] = promo.MaxDiscount ?? 0,
                    ["min_purchase"] = promo.MinPurchaseAmount ?? 0
                });

            var component = CreateFareComponent(
                name: $"Promo Discount - {promo.Code}",
                amount: -discountAmount,
                description: promo.Description);

            result.Success = true;
            result.AdjustedFare = adjustedFare;
            result.Adjustment = -discountAmount;
            result.AppliedRule = appliedRule;
            result.Components.Add(component);
            result.Explanation = GeneratePromotionExplanation(promo, discountAmount);

            context.AddStep(
                description: $"Applied promotional discount {promo.Code} ({FormatCurrency(discountAmount)} off)",
                beforeAmount: context.CurrentFare,
                afterAmount: adjustedFare,
                ruleApplied: StrategyName);

            context.CurrentFare = adjustedFare;
            context.AddRule(appliedRule);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error applying promotional discount: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    private Promotion? GetValidPromotion(string promoCode, FareCalculationRequest request)
    {
        var promo = _config.Promotions.FirstOrDefault(p => 
            p.Code.Equals(promoCode, StringComparison.OrdinalIgnoreCase) && 
            p.IsActive &&
            p.ValidFrom <= request.BookingDate &&
            p.ValidTo >= request.BookingDate);

        if (promo == null)
            return null;

        // Check route restrictions
        if (promo.ApplicableRoutes.Any() && !promo.ApplicableRoutes.Contains(request.Route))
            return null;

        // Check minimum purchase amount
        if (promo.MinPurchaseAmount.HasValue && request.BaseFare < promo.MinPurchaseAmount.Value)
            return null;

        // Check advance purchase requirements
        var daysUntilDeparture = GetDaysUntilDeparture(request.BookingDate, request.DepartureDate);
        if (promo.MinAdvanceDays.HasValue && daysUntilDeparture < promo.MinAdvanceDays.Value)
            return null;

        return promo;
    }

    private decimal CalculateDiscount(Promotion promo, decimal currentFare, FareCalculationRequest request)
    {
        decimal discount = promo.Type switch
        {
            PromotionType.Percentage => currentFare * (promo.Value / 100m),
            PromotionType.FixedAmount => promo.Value,
            PromotionType.BuyOneGetOne => currentFare * 0.5m, // 50% off for BOGO
            _ => 0m
        };

        // Apply maximum discount limit
        if (promo.MaxDiscount.HasValue && discount > promo.MaxDiscount.Value)
        {
            discount = promo.MaxDiscount.Value;
        }

        // Ensure discount doesn't exceed current fare
        if (discount > currentFare)
        {
            discount = currentFare;
        }

        return Math.Round(discount, 2);
    }

    private string GeneratePromotionExplanation(Promotion promo, decimal discountAmount)
    {
        var explanation = $"Promotional discount of {FormatCurrency(discountAmount)} applied using code '{promo.Code}'. ";
        explanation += promo.Description;

        if (promo.ValidTo < DateTime.UtcNow.AddDays(7))
        {
            explanation += $" This promotion expires on {promo.ValidTo:MMM dd, yyyy}.";
        }

        return explanation;
    }
}

public class PromotionalDiscountConfig
{
    public List<Promotion> Promotions { get; set; } = new();
}

public class Promotion
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PromotionType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinPurchaseAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> ApplicableRoutes { get; set; } = new();
    public List<string> ApplicableFareClasses { get; set; } = new();
    public int? MinAdvanceDays { get; set; }
    public int? MaxUsageCount { get; set; }
    public int CurrentUsageCount { get; set; }
    public bool IsFirstTimeCustomerOnly { get; set; }
}

public enum PromotionType
{
    Percentage,
    FixedAmount,
    BuyOneGetOne,
    FreeUpgrade,
    FreeExtras
}

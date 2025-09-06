using FlightBooking.Application.Pricing.Services;
using FlightBooking.Application.Pricing.Strategies;
using FlightBooking.Application.Pricing.Validators;
using FlightBooking.Application.Pricing.Queries;
using FlightBooking.Domain.Pricing;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlightBooking.Infrastructure.Pricing.Services;

public class PricingService : IPricingService
{
    private readonly IEnumerable<IPricingStrategy> _pricingStrategies;
    private readonly IEnumerable<IPolicyValidator> _policyValidators;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly IExtraServicesService _extraServicesService;
    private readonly IPromotionService _promotionService;
    private readonly IPricingAnalyticsService _analyticsService;
    private readonly ILogger<PricingService> _logger;

    public PricingService(
        IEnumerable<IPricingStrategy> pricingStrategies,
        IEnumerable<IPolicyValidator> policyValidators,
        ITaxCalculationService taxCalculationService,
        IExtraServicesService extraServicesService,
        IPromotionService promotionService,
        IPricingAnalyticsService analyticsService,
        ILogger<PricingService> logger)
    {
        _pricingStrategies = pricingStrategies;
        _policyValidators = policyValidators;
        _taxCalculationService = taxCalculationService;
        _extraServicesService = extraServicesService;
        _promotionService = promotionService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<FareCalculationResult> CalculateFareAsync(FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var calculationId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("Starting fare calculation {CalculationId} for flight {FlightId}", calculationId, request.FlightId);

        try
        {
            // Validate input
            ValidateRequest(request);

            // Initialize pricing context
            var context = new PricingContext
            {
                CurrentFare = request.BaseFare,
                OriginalBaseFare = request.BaseFare,
                CalculationStartTime = DateTime.UtcNow
            };

            // Step 1: Validate policies
            var policyValidation = await ValidatePoliciesAsync(request, cancellationToken);
            context.PolicyViolations.AddRange(policyValidation.Violations);

            // Check for blocking violations
            if (policyValidation.Violations.Any(v => v.IsBlocking))
            {
                return CreateFailedResult(policyValidation.Violations, calculationId, stopwatch.Elapsed);
            }

            // Step 2: Apply pricing strategies in priority order
            var applicableStrategies = _pricingStrategies
                .Where(s => s.CanApply(request, context))
                .OrderBy(s => s.Priority)
                .ToList();

            _logger.LogDebug("Applying {StrategyCount} pricing strategies for calculation {CalculationId}", 
                applicableStrategies.Count, calculationId);

            var fareComponents = new List<FareComponent>();

            foreach (var strategy in applicableStrategies)
            {
                try
                {
                    var strategyResult = await strategy.ApplyAsync(request, context, cancellationToken);

                    if (strategyResult.Success && strategyResult.AppliedRule != null)
                    {
                        context.AddRule(strategyResult.AppliedRule);
                        fareComponents.AddRange(strategyResult.Components);

                        _logger.LogDebug("Applied strategy {StrategyName} with impact {Impact} for calculation {CalculationId}",
                            strategy.StrategyName, strategyResult.Adjustment, calculationId);
                    }
                    else if (!strategyResult.Success)
                    {
                        _logger.LogWarning("Strategy {StrategyName} failed: {Error} for calculation {CalculationId}",
                            strategy.StrategyName, strategyResult.ErrorMessage, calculationId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying strategy {StrategyName} for calculation {CalculationId}", 
                        strategy.StrategyName, calculationId);
                }
            }

            // Step 3: Calculate taxes and fees
            var taxes = await _taxCalculationService.CalculateTaxesAsync(request.Route, request.FareClassId.ToString(), context.CurrentFare, cancellationToken);
            var fees = await _taxCalculationService.CalculateFeesAsync(request.Route, request.FareClassId.ToString(), context.CurrentFare, cancellationToken);

            // Step 4: Calculate extra services
            var extraServices = new List<ExtraServiceCharge>();
            if (request.RequestedExtras.Any())
            {
                extraServices = await _extraServicesService.CalculateServiceChargesAsync(request.RequestedExtras, request, cancellationToken);
            }

            // Step 5: Build fare breakdown
            var fareBreakdown = BuildFareBreakdown(request, context, fareComponents, taxes, fees, extraServices);

            // Step 6: Generate explanation
            var explanation = await GeneratePricingExplanationAsync(request, context, fareBreakdown, cancellationToken);

            // Step 7: Create result
            var result = new FareCalculationResult
            {
                Success = true,
                FareBreakdown = fareBreakdown,
                PolicyViolations = context.PolicyViolations,
                AppliedRules = context.AppliedRules,
                Explanation = explanation,
                CalculatedAt = DateTime.UtcNow,
                CalculationDuration = stopwatch.Elapsed,
                CalculationId = calculationId
            };

            _logger.LogInformation("Completed fare calculation {CalculationId} in {Duration}ms. Final fare: {FinalFare}", 
                calculationId, stopwatch.ElapsedMilliseconds, fareBreakdown.GrandTotal);

            return result;
        }
        catch (ArgumentException)
        {
            // Re-throw validation errors
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fare calculation {CalculationId}", calculationId);

            return new FareCalculationResult
            {
                Success = false,
                ErrorMessage = $"An error occurred during fare calculation: {ex.Message}",
                CalculationId = calculationId,
                CalculatedAt = DateTime.UtcNow,
                CalculationDuration = stopwatch.Elapsed
            };
        }
    }

    public async Task<PolicyValidationResult> ValidatePoliciesAsync(FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        var result = new PolicyValidationResult();
        var context = new PricingContext
        {
            CurrentFare = request.BaseFare,
            OriginalBaseFare = request.BaseFare
        };

        foreach (var validator in _policyValidators)
        {
            try
            {
                var validationResult = await validator.ValidateAsync(request, context, cancellationToken);
                
                result.Violations.AddRange(validationResult.Violations);
                result.Warnings.AddRange(validationResult.Warnings);
                result.Recommendations.AddRange(validationResult.Recommendations);
                result.RequiredDocuments.AddRange(validationResult.RequiredDocuments);

                if (validationResult.RequiresApproval)
                {
                    result.RequiresApproval = true;
                    result.ApprovalReason = validationResult.ApprovalReason;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in policy validator {ValidatorName}", validator.ValidatorName);
                
                result.Violations.Add(new PolicyViolation
                {
                    PolicyId = validator.ValidatorName,
                    PolicyName = validator.ValidatorName,
                    ViolationType = "ValidationError",
                    Description = $"Error validating policy: {ex.Message}",
                    Severity = PolicySeverity.Critical,
                    IsBlocking = true
                });
            }
        }

        result.IsValid = !result.Violations.Any(v => v.IsBlocking);
        return result;
    }

    public async Task<TaxAndFeeBreakdown> GetTaxAndFeeBreakdownAsync(string route, string fareClass, decimal baseFare, CancellationToken cancellationToken = default)
    {
        var taxes = await _taxCalculationService.CalculateTaxesAsync(route, fareClass, baseFare, cancellationToken);
        var fees = await _taxCalculationService.CalculateFeesAsync(route, fareClass, baseFare, cancellationToken);

        return new TaxAndFeeBreakdown
        {
            Taxes = taxes,
            Fees = fees,
            TotalTaxes = taxes.Sum(t => t.Amount),
            TotalFees = fees.Sum(f => f.Amount),
            GrandTotal = baseFare + taxes.Sum(t => t.Amount) + fees.Sum(f => f.Amount)
        };
    }

    public async Task<ExtraServicesCalculation> CalculateExtraServicesAsync(List<ExtraService> services, FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        var serviceCharges = await _extraServicesService.CalculateServiceChargesAsync(services, request, cancellationToken);
        var bundleDiscounts = await _extraServicesService.GetBundleDiscountsAsync(services, cancellationToken);

        return new ExtraServicesCalculation
        {
            ServiceCharges = serviceCharges,
            TotalCharges = serviceCharges.Sum(sc => sc.TotalPrice),
            BundleDiscounts = bundleDiscounts,
            Currency = request.Currency
        };
    }

    public async Task<List<AvailablePromotion>> GetAvailablePromotionsAsync(string route, DateTime departureDate, CancellationToken cancellationToken = default)
    {
        return await _promotionService.GetAvailablePromotionsAsync(route, departureDate, cancellationToken);
    }

    public async Task<PromoCodeValidationResult> ValidatePromoCodeAsync(string promoCode, FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        return await _promotionService.ValidatePromoCodeAsync(promoCode, request, cancellationToken);
    }

    public async Task<PricingEducation> GetPricingEducationAsync(FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await CalculateFareAsync(request, cancellationToken);
        return await _analyticsService.GeneratePricingEducationAsync(request, result, cancellationToken);
    }

    public async Task<List<PricingScenario>> SimulatePricingScenariosAsync(FareCalculationRequest baseRequest, List<ScenarioParameter> scenarios, CancellationToken cancellationToken = default)
    {
        return await _analyticsService.SimulateScenariosAsync(baseRequest, scenarios, cancellationToken);
    }

    private void ValidateRequest(FareCalculationRequest request)
    {
        if (request.FlightId == Guid.Empty)
            throw new ArgumentException("FlightId is required", nameof(request));

        if (request.FareClassId == Guid.Empty)
            throw new ArgumentException("FareClassId is required", nameof(request));

        if (request.BaseFare <= 0)
            throw new ArgumentException("BaseFare must be greater than zero", nameof(request));

        if (request.PassengerCount <= 0)
            throw new ArgumentException("PassengerCount must be greater than zero", nameof(request));

        if (request.DepartureDate < request.BookingDate)
            throw new ArgumentException("DepartureDate must be after BookingDate", nameof(request));

        if (string.IsNullOrWhiteSpace(request.DepartureAirport))
            throw new ArgumentException("DepartureAirport is required", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ArrivalAirport))
            throw new ArgumentException("ArrivalAirport is required", nameof(request));
    }

    private FareBreakdown BuildFareBreakdown(
        FareCalculationRequest request,
        PricingContext context,
        List<FareComponent> components,
        List<Tax> taxes,
        List<Fee> fees,
        List<ExtraServiceCharge> extraServices)
    {
        var breakdown = new FareBreakdown
        {
            BaseFare = request.BaseFare,
            AdjustedBaseFare = context.CurrentFare,
            Components = components,
            Taxes = taxes,
            Fees = fees,
            Extras = extraServices,
            Currency = request.Currency
        };

        // Calculate totals
        breakdown.SubTotal = breakdown.AdjustedBaseFare;
        breakdown.TotalTaxes = taxes.Sum(t => t.Amount);
        breakdown.TotalFees = fees.Sum(f => f.Amount);
        breakdown.TotalExtras = extraServices.Sum(e => e.TotalPrice);
        breakdown.TotalDiscount = components.Where(c => c.Amount < 0).Sum(c => Math.Abs(c.Amount));
        breakdown.GrandTotal = breakdown.SubTotal + breakdown.TotalTaxes + breakdown.TotalFees + breakdown.TotalExtras;

        return breakdown;
    }

    private async Task<PricingExplanation> GeneratePricingExplanationAsync(
        FareCalculationRequest request,
        PricingContext context,
        FareBreakdown breakdown,
        CancellationToken cancellationToken)
    {
        var explanation = new PricingExplanation();

        // Generate summary
        var totalAdjustment = breakdown.AdjustedBaseFare - breakdown.BaseFare;
        if (totalAdjustment > 0)
        {
            explanation.Summary = $"Your fare includes a {totalAdjustment:C} adjustment due to various pricing factors.";
        }
        else if (totalAdjustment < 0)
        {
            explanation.Summary = $"You saved {Math.Abs(totalAdjustment):C} due to applicable discounts.";
        }
        else
        {
            explanation.Summary = "Your fare is based on standard pricing with no adjustments.";
        }

        // Add key factors
        explanation.KeyFactors = context.AppliedRules
            .OrderByDescending(r => Math.Abs(r.Impact))
            .Take(5)
            .Select(r => $"{r.RuleName}: {(r.Impact >= 0 ? "+" : "")}{r.Impact:C}")
            .ToList();

        // Add pricing steps
        explanation.Steps = context.Steps;

        // Generate recommendations
        explanation.Recommendations = await GenerateRecommendationsAsync(request, context, cancellationToken);

        // Add metadata
        explanation.Metadata["total_rules_applied"] = context.AppliedRules.Count;
        explanation.Metadata["calculation_complexity"] = context.Steps.Count;
        explanation.Metadata["base_fare_adjustment_percentage"] = breakdown.BaseFare > 0 ?
            ((breakdown.AdjustedBaseFare - breakdown.BaseFare) / breakdown.BaseFare * 100) : 0;

        return explanation;
    }

    private async Task<List<string>> GenerateRecommendationsAsync(
        FareCalculationRequest request,
        PricingContext context,
        CancellationToken cancellationToken)
    {
        var recommendations = new List<string>();

        try
        {
            var savingsTips = await _analyticsService.GetSavingsTipsAsync(request, cancellationToken);
            recommendations.AddRange(savingsTips.Take(3).Select(tip => tip.Description));

            // Add specific recommendations based on applied rules
            if (context.AppliedRules.Any(r => r.RuleType == "WeekendSurcharge"))
            {
                recommendations.Add("Consider traveling on weekdays to avoid weekend surcharges.");
            }

            if (context.AppliedRules.Any(r => r.RuleType == "DemandBased"))
            {
                recommendations.Add("Book earlier or consider alternative dates for better pricing.");
            }

            if (!context.AppliedRules.Any(r => r.RuleType == "PromotionalDiscount"))
            {
                var promos = await _promotionService.GetAvailablePromotionsAsync(request.Route, request.DepartureDate, cancellationToken);
                if (promos.Any())
                {
                    recommendations.Add($"Check available promotional codes - you could save up to {promos.Max(p => p.EstimatedSavings):C}.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating recommendations for calculation");
        }

        return recommendations;
    }

    private FareCalculationResult CreateFailedResult(List<PolicyViolation> violations, string calculationId, TimeSpan duration)
    {
        var blockingViolations = violations.Where(v => v.IsBlocking).ToList();
        var errorMessage = blockingViolations.Count == 1
            ? blockingViolations.First().Description
            : $"Multiple policy violations prevent booking: {string.Join("; ", blockingViolations.Select(v => v.Description))}";

        return new FareCalculationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            PolicyViolations = violations,
            CalculationId = calculationId,
            CalculatedAt = DateTime.UtcNow,
            CalculationDuration = duration
        };
    }

    public async Task<FareCalculationResult> CalculatePricingAsync(CalculatePricingQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating pricing for flight {FlightId} with {PassengerCount} passengers",
            query.FlightId, query.PassengerCount);

        try
        {
            // Create a simplified FareCalculationRequest from the query
            var request = new FareCalculationRequest
            {
                FlightId = query.FlightId,
                PassengerCount = query.PassengerCount,
                BookingDate = query.BookingDate,
                PromoCode = query.PromoCode,
                Currency = query.Currency
            };

            // Use the existing CalculateFareAsync method
            return await CalculateFareAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating pricing for flight {FlightId}", query.FlightId);
            throw;
        }
    }
}

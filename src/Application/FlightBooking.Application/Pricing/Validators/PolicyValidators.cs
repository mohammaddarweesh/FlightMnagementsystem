using FlightBooking.Application.Pricing.Strategies;
using FlightBooking.Domain.Pricing;

namespace FlightBooking.Application.Pricing.Validators;

public class AdvancePurchasePolicyValidator : BasePolicyValidator
{
    public override string ValidatorName => "AdvancePurchasePolicy";
    public override PolicyType PolicyType => PolicyType.AdvancePurchase;

    private readonly AdvancePurchasePolicyConfig _config;

    public AdvancePurchasePolicyValidator(AdvancePurchasePolicyConfig config)
    {
        _config = config;
    }

    public override Task<PolicyValidationResult> ValidateAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default)
    {
        var result = new PolicyValidationResult();
        
        try
        {
            var daysUntilDeparture = (int)(request.DepartureDate.Date - request.BookingDate.Date).TotalDays;
            var policy = GetApplicablePolicy(request);

            if (policy == null)
            {
                return Task.FromResult(result);
            }

            // Check minimum advance purchase requirement
            if (daysUntilDeparture < policy.MinimumDaysAdvance)
            {
                var violation = CreateViolation(
                    policyId: "ADV_PURCHASE_MIN",
                    policyName: "Minimum Advance Purchase",
                    violationType: "InsufficientAdvancePurchase",
                    description: $"Booking must be made at least {policy.MinimumDaysAdvance} days in advance. Current booking is {daysUntilDeparture} days in advance.",
                    severity: PolicySeverity.Error,
                    isBlocking: true,
                    resolution: policy.AllowSameDayBooking && daysUntilDeparture == 0 ? 
                        $"Same-day booking allowed with surcharge of {policy.SameDayBookingSurcharge:C}" : 
                        "Please select a departure date that meets the advance purchase requirement");

                result.Violations.Add(violation);
                result.IsValid = false;

                // Add same-day booking option if available
                if (policy.AllowSameDayBooking && daysUntilDeparture == 0)
                {
                    var recommendation = CreateRecommendation(
                        type: "SameDayBooking",
                        message: $"Same-day booking available with additional surcharge of {policy.SameDayBookingSurcharge:C}",
                        potentialSavings: null,
                        alternativeAction: "Accept same-day booking surcharge",
                        priority: 1);

                    result.Recommendations.Add(recommendation);
                    result.IsValid = true; // Allow with surcharge
                }
            }

            // Check maximum advance purchase (if applicable)
            if (policy.MaximumDaysAdvance.HasValue && daysUntilDeparture > policy.MaximumDaysAdvance.Value)
            {
                var warning = CreateWarning(
                    policyId: "ADV_PURCHASE_MAX",
                    message: $"Booking is {daysUntilDeparture} days in advance, which exceeds the recommended maximum of {policy.MaximumDaysAdvance.Value} days",
                    suggestion: "Consider booking closer to departure date for potentially better rates",
                    canProceed: true);

                result.Warnings.Add(warning);
            }

            // Add recommendations for better pricing
            if (daysUntilDeparture >= policy.MinimumDaysAdvance && daysUntilDeparture < 21)
            {
                var recommendation = CreateRecommendation(
                    type: "EarlyBookingDiscount",
                    message: "Book 21+ days in advance to qualify for early booking discounts",
                    potentialSavings: request.BaseFare * 0.1m, // Estimate 10% savings
                    alternativeAction: "Consider flexible dates for better rates",
                    priority: 2);

                result.Recommendations.Add(recommendation);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            var violation = CreateViolation(
                policyId: "ADV_PURCHASE_ERROR",
                policyName: "Advance Purchase Validation Error",
                violationType: "ValidationError",
                description: $"Error validating advance purchase policy: {ex.Message}",
                severity: PolicySeverity.Critical,
                isBlocking: true);

            result.Violations.Add(violation);
        }

        return Task.FromResult(result);
    }

    private AdvancePurchasePolicy? GetApplicablePolicy(FareCalculationRequest request)
    {
        return _config.Policies.FirstOrDefault(p => 
            (p.ApplicableFareClasses.Count == 0 || p.ApplicableFareClasses.Contains(request.FareClassId.ToString())) &&
            (p.ExemptRoutes.Count == 0 || !p.ExemptRoutes.Contains(request.Route)));
    }
}

public class BlackoutDatePolicyValidator : BasePolicyValidator
{
    public override string ValidatorName => "BlackoutDatePolicy";
    public override PolicyType PolicyType => PolicyType.BlackoutDates;

    private readonly BlackoutDatePolicyConfig _config;

    public BlackoutDatePolicyValidator(BlackoutDatePolicyConfig config)
    {
        _config = config;
    }

    public override Task<PolicyValidationResult> ValidateAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default)
    {
        var result = new PolicyValidationResult();
        
        try
        {
            var applicablePolicies = GetApplicablePolicies(request);

            foreach (var policy in applicablePolicies)
            {
                var blackoutPeriod = policy.BlackoutPeriods.FirstOrDefault(bp => bp.Contains(request.DepartureDate));
                
                if (blackoutPeriod != null)
                {
                    switch (policy.Type)
                    {
                        case BlackoutType.NoBooking:
                            var violation = CreateViolation(
                                policyId: "BLACKOUT_NO_BOOKING",
                                policyName: "Blackout Period - No Booking",
                                violationType: "BlackoutPeriod",
                                description: $"Bookings are not allowed during {blackoutPeriod.Description} ({blackoutPeriod.StartDate:MMM dd} - {blackoutPeriod.EndDate:MMM dd})",
                                severity: PolicySeverity.Error,
                                isBlocking: true,
                                resolution: policy.AllowOverride ? 
                                    $"Override available with surcharge of {policy.OverrideSurcharge:C}" : 
                                    "Please select a different travel date");

                            result.Violations.Add(violation);
                            result.IsValid = !policy.AllowOverride;
                            break;

                        case BlackoutType.NoDiscount:
                            var warning = CreateWarning(
                                policyId: "BLACKOUT_NO_DISCOUNT",
                                message: $"Promotional discounts are not available during {blackoutPeriod.Description}",
                                suggestion: "Consider traveling on different dates for discount eligibility",
                                canProceed: true);

                            result.Warnings.Add(warning);
                            break;

                        case BlackoutType.SurchargePeriod:
                            var surchargeWarning = CreateWarning(
                                policyId: "BLACKOUT_SURCHARGE",
                                message: $"Additional surcharge applies during {blackoutPeriod.Description}",
                                suggestion: $"Peak period surcharge of {policy.OverrideSurcharge:C} will be applied",
                                canProceed: true);

                            result.Warnings.Add(surchargeWarning);
                            break;
                    }
                }
            }

            // Check return date for round trips
            if (request.IsRoundTrip && request.ReturnDate.HasValue)
            {
                foreach (var policy in applicablePolicies)
                {
                    var returnBlackoutPeriod = policy.BlackoutPeriods.FirstOrDefault(bp => bp.Contains(request.ReturnDate.Value));
                    
                    if (returnBlackoutPeriod != null && policy.Type == BlackoutType.NoBooking)
                    {
                        var violation = CreateViolation(
                            policyId: "BLACKOUT_RETURN_NO_BOOKING",
                            policyName: "Blackout Period - Return Date",
                            violationType: "BlackoutPeriod",
                            description: $"Return bookings are not allowed during {returnBlackoutPeriod.Description}",
                            severity: PolicySeverity.Error,
                            isBlocking: true);

                        result.Violations.Add(violation);
                        result.IsValid = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            var violation = CreateViolation(
                policyId: "BLACKOUT_ERROR",
                policyName: "Blackout Date Validation Error",
                violationType: "ValidationError",
                description: $"Error validating blackout date policy: {ex.Message}",
                severity: PolicySeverity.Critical,
                isBlocking: true);

            result.Violations.Add(violation);
        }

        return Task.FromResult(result);
    }

    private List<BlackoutDatePolicy> GetApplicablePolicies(FareCalculationRequest request)
    {
        return _config.Policies.Where(p => 
            (p.AffectedRoutes.Count == 0 || p.AffectedRoutes.Contains(request.Route)) &&
            (p.AffectedFareClasses.Count == 0 || p.AffectedFareClasses.Contains(request.FareClassId.ToString()))).ToList();
    }
}

public class RouteRestrictionPolicyValidator : BasePolicyValidator
{
    public override string ValidatorName => "RouteRestrictionPolicy";
    public override PolicyType PolicyType => PolicyType.RouteRestriction;

    private readonly RouteRestrictionPolicyConfig _config;

    public RouteRestrictionPolicyValidator(RouteRestrictionPolicyConfig config)
    {
        _config = config;
    }

    public override Task<PolicyValidationResult> ValidateAsync(FareCalculationRequest request, PricingContext context, CancellationToken cancellationToken = default)
    {
        var result = new PolicyValidationResult();
        
        try
        {
            var applicablePolicies = GetApplicablePolicies(request);

            foreach (var policy in applicablePolicies)
            {
                switch (policy.Type)
                {
                    case RestrictionType.Prohibited:
                        var violation = CreateViolation(
                            policyId: "ROUTE_PROHIBITED",
                            policyName: "Route Restriction - Prohibited",
                            violationType: "RouteProhibited",
                            description: $"Travel on route {request.Route} is currently prohibited. Reason: {policy.Reason}",
                            severity: PolicySeverity.Error,
                            isBlocking: true,
                            resolution: policy.TemporaryUntil.HasValue ? 
                                $"Restriction is temporary until {policy.TemporaryUntil:MMM dd, yyyy}" : 
                                "Please select an alternative route");

                        result.Violations.Add(violation);
                        result.IsValid = false;
                        break;

                    case RestrictionType.RequiresApproval:
                        result.RequiresApproval = true;
                        result.ApprovalReason = $"Route {request.Route} requires special approval: {policy.Reason}";
                        
                        var approvalWarning = CreateWarning(
                            policyId: "ROUTE_APPROVAL_REQUIRED",
                            message: $"This route requires approval: {policy.Reason}",
                            suggestion: "Booking will be held pending approval",
                            canProceed: true);

                        result.Warnings.Add(approvalWarning);
                        break;

                    case RestrictionType.DocumentRequired:
                        result.RequiredDocuments.Add($"Special documentation required for route {request.Route}: {policy.Reason}");
                        
                        var docWarning = CreateWarning(
                            policyId: "ROUTE_DOCUMENT_REQUIRED",
                            message: $"Additional documentation required: {policy.Reason}",
                            suggestion: "Ensure you have required documents before travel",
                            canProceed: true);

                        result.Warnings.Add(docWarning);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            var violation = CreateViolation(
                policyId: "ROUTE_RESTRICTION_ERROR",
                policyName: "Route Restriction Validation Error",
                violationType: "ValidationError",
                description: $"Error validating route restriction policy: {ex.Message}",
                severity: PolicySeverity.Critical,
                isBlocking: true);

            result.Violations.Add(violation);
        }

        return Task.FromResult(result);
    }

    private List<RouteRestrictionPolicy> GetApplicablePolicies(FareCalculationRequest request)
    {
        return _config.Policies.Where(p => 
            p.RestrictedRoutes.Contains(request.Route) ||
            p.AllowedOrigins.Any() && !p.AllowedOrigins.Contains(request.DepartureAirport) ||
            p.AllowedDestinations.Any() && !p.AllowedDestinations.Contains(request.ArrivalAirport)).ToList();
    }
}

// Configuration classes
public class AdvancePurchasePolicyConfig
{
    public List<AdvancePurchasePolicy> Policies { get; set; } = new();
}

public class BlackoutDatePolicyConfig
{
    public List<BlackoutDatePolicy> Policies { get; set; } = new();
}

public class RouteRestrictionPolicyConfig
{
    public List<RouteRestrictionPolicy> Policies { get; set; } = new();
}

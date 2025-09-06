using FlightBooking.Application.Pricing.Strategies;
using FlightBooking.Application.Pricing.Validators;
using FlightBooking.Domain.Pricing;
using Xunit;

namespace FlightBooking.UnitTests.Pricing;

public class AdvancePurchasePolicyValidatorTests
{
    private readonly AdvancePurchasePolicyValidator _validator;
    private readonly AdvancePurchasePolicyConfig _config;

    public AdvancePurchasePolicyValidatorTests()
    {
        _config = new AdvancePurchasePolicyConfig
        {
            Policies = new List<AdvancePurchasePolicy>
            {
                new()
                {
                    MinimumDaysAdvance = 7,
                    MaximumDaysAdvance = 365,
                    ApplicableFareClasses = new List<string>(),
                    ExemptRoutes = new List<string>(),
                    AllowSameDayBooking = true,
                    SameDayBookingSurcharge = 100m
                }
            }
        };
        _validator = new AdvancePurchasePolicyValidator(_config);
    }

    [Fact]
    public async Task ValidateAsync_SufficientAdvancePurchase_ShouldPass()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(14), // 14 days advance
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task ValidateAsync_InsufficientAdvancePurchase_ShouldFail()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(3), // Only 3 days advance
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Violations);
        Assert.Equal("ADV_PURCHASE_MIN", result.Violations[0].PolicyId);
        Assert.True(result.Violations[0].IsBlocking);
    }

    [Fact]
    public async Task ValidateAsync_SameDayBookingAllowed_ShouldPassWithSurcharge()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today, // Same day
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid); // Should be valid with surcharge option
        Assert.Single(result.Violations); // Still has violation but not blocking
        Assert.Single(result.Recommendations); // Should have same-day booking recommendation
        Assert.Contains("Same-day booking available", result.Recommendations[0].Message);
    }

    [Fact]
    public async Task ValidateAsync_ExcessiveAdvancePurchase_ShouldWarn()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(400), // 400 days advance
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid); // Valid but with warning
        Assert.Single(result.Warnings);
        Assert.Equal("ADV_PURCHASE_MAX", result.Warnings[0].PolicyId);
        Assert.True(result.Warnings[0].CanProceed);
    }

    [Fact]
    public async Task ValidateAsync_EarlyBookingRecommendation_ShouldSuggestBetterRates()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(14), // 14 days advance
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Recommendations);
        Assert.Equal("EarlyBookingDiscount", result.Recommendations[0].Type);
        Assert.Contains("21+ days in advance", result.Recommendations[0].Message);
        Assert.Equal(50m, result.Recommendations[0].PotentialSavings); // 10% of 500
    }
}

public class BlackoutDatePolicyValidatorTests
{
    private readonly BlackoutDatePolicyValidator _validator;
    private readonly BlackoutDatePolicyConfig _config;

    public BlackoutDatePolicyValidatorTests()
    {
        _config = new BlackoutDatePolicyConfig
        {
            Policies = new List<BlackoutDatePolicy>
            {
                new()
                {
                    BlackoutPeriods = new List<DateRange>
                    {
                        new()
                        {
                            StartDate = new DateTime(2025, 12, 20),
                            EndDate = new DateTime(2025, 12, 31),
                            Description = "Christmas Holiday Period"
                        }
                    },
                    AffectedRoutes = new List<string>(),
                    AffectedFareClasses = new List<string>(),
                    Type = BlackoutType.NoBooking,
                    Reason = "High demand holiday period",
                    AllowOverride = true,
                    OverrideSurcharge = 200m
                },
                new()
                {
                    BlackoutPeriods = new List<DateRange>
                    {
                        new()
                        {
                            StartDate = new DateTime(2025, 7, 1),
                            EndDate = new DateTime(2025, 8, 31),
                            Description = "Summer Peak Season"
                        }
                    },
                    Type = BlackoutType.NoDiscount,
                    Reason = "Peak summer travel"
                }
            }
        };
        _validator = new BlackoutDatePolicyValidator(_config);
    }

    [Fact]
    public async Task ValidateAsync_NonBlackoutDate_ShouldPass()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 10, 15), // Non-blackout date
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ValidateAsync_NoBookingBlackoutDate_ShouldFail()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 12, 25), // Christmas
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid); // Override available, so not completely blocked
        Assert.Single(result.Violations);
        Assert.Equal("BLACKOUT_NO_BOOKING", result.Violations[0].PolicyId);
        Assert.Contains("Christmas Holiday Period", result.Violations[0].Description);
        Assert.Contains("Override available", result.Violations[0].Resolution);
    }

    [Fact]
    public async Task ValidateAsync_NoDiscountBlackoutDate_ShouldWarn()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 7, 15), // Summer peak
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
        Assert.Single(result.Warnings);
        Assert.Equal("BLACKOUT_NO_DISCOUNT", result.Warnings[0].PolicyId);
        Assert.Contains("Promotional discounts are not available", result.Warnings[0].Message);
    }

    [Fact]
    public async Task ValidateAsync_RoundTripReturnBlackout_ShouldFail()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 10, 15), // Valid departure
            IsRoundTrip = true,
            ReturnDate = new DateTime(2025, 12, 25), // Christmas return
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Violations);
        Assert.Equal("BLACKOUT_RETURN_NO_BOOKING", result.Violations[0].PolicyId);
        Assert.True(result.Violations[0].IsBlocking);
    }
}

public class RouteRestrictionPolicyValidatorTests
{
    private readonly RouteRestrictionPolicyValidator _validator;
    private readonly RouteRestrictionPolicyConfig _config;

    public RouteRestrictionPolicyValidatorTests()
    {
        _config = new RouteRestrictionPolicyConfig
        {
            Policies = new List<RouteRestrictionPolicy>
            {
                new()
                {
                    RestrictedRoutes = new List<string> { "JFK-LAX" },
                    Type = RestrictionType.Prohibited,
                    Reason = "Route temporarily suspended",
                    TemporaryUntil = DateTime.Today.AddDays(30)
                },
                new()
                {
                    RestrictedRoutes = new List<string> { "NYC-LON" },
                    Type = RestrictionType.RequiresApproval,
                    Reason = "International route requires special approval"
                },
                new()
                {
                    RestrictedRoutes = new List<string> { "LAX-NRT" },
                    Type = RestrictionType.DocumentRequired,
                    Reason = "Visa and health documentation required"
                }
            }
        };
        _validator = new RouteRestrictionPolicyValidator(_config);
    }

    [Fact]
    public async Task ValidateAsync_UnrestrictedRoute_ShouldPass()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            Route = "SFO-SEA",
            DepartureAirport = "SFO",
            ArrivalAirport = "SEA",
            BaseFare = 300m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ValidateAsync_ProhibitedRoute_ShouldFail()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            Route = "JFK-LAX",
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            BaseFare = 500m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Violations);
        Assert.Equal("ROUTE_PROHIBITED", result.Violations[0].PolicyId);
        Assert.True(result.Violations[0].IsBlocking);
        Assert.Contains("temporarily suspended", result.Violations[0].Description);
        Assert.Contains("temporary until", result.Violations[0].Resolution);
    }

    [Fact]
    public async Task ValidateAsync_ApprovalRequiredRoute_ShouldRequireApproval()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            Route = "NYC-LON",
            DepartureAirport = "NYC",
            ArrivalAirport = "LON",
            BaseFare = 800m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.RequiresApproval);
        Assert.Equal("Route NYC-LON requires special approval: International route requires special approval", result.ApprovalReason);
        Assert.Single(result.Warnings);
        Assert.Equal("ROUTE_APPROVAL_REQUIRED", result.Warnings[0].PolicyId);
    }

    [Fact]
    public async Task ValidateAsync_DocumentRequiredRoute_ShouldRequireDocuments()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            Route = "LAX-NRT",
            DepartureAirport = "LAX",
            ArrivalAirport = "NRT",
            BaseFare = 1200m
        };
        var context = new PricingContext();

        // Act
        var result = await _validator.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.RequiredDocuments);
        Assert.Contains("Visa and health documentation", result.RequiredDocuments[0]);
        Assert.Single(result.Warnings);
        Assert.Equal("ROUTE_DOCUMENT_REQUIRED", result.Warnings[0].PolicyId);
    }
}

public class PricingContextTests
{
    [Fact]
    public void AddStep_ShouldAddStepWithCorrectOrder()
    {
        // Arrange
        var context = new PricingContext();

        // Act
        context.AddStep("First step", 100m, 110m, "Rule1");
        context.AddStep("Second step", 110m, 120m, "Rule2");

        // Assert
        Assert.Equal(2, context.Steps.Count);
        Assert.Equal(1, context.Steps[0].Order);
        Assert.Equal(2, context.Steps[1].Order);
        Assert.Equal(10m, context.Steps[0].Change);
        Assert.Equal("Increase", context.Steps[0].ChangeType);
    }

    [Fact]
    public void AddRule_ShouldAddRuleAndUpdateMetadata()
    {
        // Arrange
        var context = new PricingContext();
        var rule = new AppliedRule
        {
            RuleId = "TEST_RULE",
            RuleName = "Test Rule",
            Impact = 50m
        };

        // Act
        context.AddRule(rule);

        // Assert
        Assert.Single(context.AppliedRules);
        Assert.True(context.HasRuleBeenApplied("TEST_RULE"));
        Assert.True((bool)context.Metadata["rule_TEST_RULE_applied"]);
        Assert.Equal(50m, context.Metadata["rule_TEST_RULE_impact"]);
    }

    [Fact]
    public void GetTotalAdjustment_ShouldCalculateCorrectly()
    {
        // Arrange
        var context = new PricingContext
        {
            OriginalBaseFare = 500m,
            CurrentFare = 575m
        };

        // Act
        var adjustment = context.GetTotalAdjustment();

        // Assert
        Assert.Equal(75m, adjustment);
    }

    [Fact]
    public void GetTotalMultiplier_ShouldCalculateCorrectly()
    {
        // Arrange
        var context = new PricingContext
        {
            OriginalBaseFare = 500m,
            CurrentFare = 575m
        };

        // Act
        var multiplier = context.GetTotalMultiplier();

        // Assert
        Assert.Equal(1.15m, multiplier);
    }
}

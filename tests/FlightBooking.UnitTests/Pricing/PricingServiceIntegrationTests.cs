using FlightBooking.Application.Pricing.Services;
using FlightBooking.Application.Pricing.Strategies;
using FlightBooking.Application.Pricing.Validators;
using FlightBooking.Domain.Pricing;
using FlightBooking.Infrastructure.Pricing.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlightBooking.UnitTests.Pricing;

public class PricingServiceIntegrationTests
{
    private readonly Mock<ITaxCalculationService> _mockTaxService;
    private readonly Mock<IExtraServicesService> _mockExtraServicesService;
    private readonly Mock<IPromotionService> _mockPromotionService;
    private readonly Mock<IPricingAnalyticsService> _mockAnalyticsService;
    private readonly Mock<ILogger<PricingService>> _mockLogger;
    private readonly PricingService _pricingService;

    public PricingServiceIntegrationTests()
    {
        _mockTaxService = new Mock<ITaxCalculationService>();
        _mockExtraServicesService = new Mock<IExtraServicesService>();
        _mockPromotionService = new Mock<IPromotionService>();
        _mockAnalyticsService = new Mock<IPricingAnalyticsService>();
        _mockLogger = new Mock<ILogger<PricingService>>();

        // Setup strategies
        var strategies = new List<IPricingStrategy>
        {
            new WeekendSurchargeStrategy(new WeekendSurchargeConfig { SurchargePercentage = 15.0m }),
            new SeasonalMultiplierStrategy(new SeasonalMultiplierConfig { SummerMultiplier = 1.25m }),
            new DemandBasedStrategy(new DemandBasedConfig 
            { 
                LoadFactorTiers = new List<LoadFactorTier>
                {
                    new() { Name = "High", MinLoadFactor = 0.85, Multiplier = 1.3m }
                }
            }),
            new PromotionalDiscountStrategy(new PromotionalDiscountConfig
            {
                Promotions = new List<Promotion>
                {
                    new()
                    {
                        Code = "SAVE20",
                        Type = PromotionType.Percentage,
                        Value = 20m,
                        ValidFrom = new DateTime(2025, 1, 1),
                        ValidTo = new DateTime(2025, 12, 31),
                        IsActive = true
                    }
                }
            })
        };

        // Setup validators
        var validators = new List<IPolicyValidator>
        {
            new AdvancePurchasePolicyValidator(new AdvancePurchasePolicyConfig
            {
                Policies = new List<AdvancePurchasePolicy>
                {
                    new() { MinimumDaysAdvance = 7, AllowSameDayBooking = false }
                }
            })
        };

        // Setup mock services
        _mockTaxService.Setup(x => x.CalculateTaxesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tax>
            {
                new() { Code = "TAX1", Name = "Airport Tax", Amount = 25m, Type = TaxType.AirportTax }
            });

        _mockTaxService.Setup(x => x.CalculateFeesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fee>
            {
                new() { Code = "FEE1", Name = "Booking Fee", Amount = 15m, Type = FeeType.BookingFee }
            });

        _mockExtraServicesService.Setup(x => x.CalculateServiceChargesAsync(It.IsAny<List<ExtraService>>(), It.IsAny<FareCalculationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtraServiceCharge>());

        _mockAnalyticsService.Setup(x => x.GetSavingsTipsAsync(It.IsAny<FareCalculationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SavingsTip>());

        _pricingService = new PricingService(
            strategies,
            validators,
            _mockTaxService.Object,
            _mockExtraServicesService.Object,
            _mockPromotionService.Object,
            _mockAnalyticsService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CalculateFareAsync_WeekdayRegularSeason_ShouldReturnBaseFareWithTaxesAndFees()
    {
        // Arrange - Regular weekday flight
        var request = new FareCalculationRequest
        {
            FlightId = Guid.NewGuid(),
            FareClassId = Guid.NewGuid(),
            BaseFare = 500m,
            DepartureDate = new DateTime(2025, 5, 15), // Wednesday in May
            BookingDate = new DateTime(2025, 5, 1), // Booked 2 weeks in advance
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            Route = "JFK-LAX",
            PassengerCount = 1,
            CurrentLoadFactor = 0.5 // Low demand
        };

        // Act
        var result = await _pricingService.CalculateFareAsync(request);

        // Assert
        Assert.True(result.Success, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal(500m, result.FareBreakdown.AdjustedBaseFare); // No adjustments
        Assert.Equal(25m, result.FareBreakdown.TotalTaxes);
        Assert.Equal(15m, result.FareBreakdown.TotalFees);
        Assert.Equal(540m, result.FareBreakdown.GrandTotal); // 500 + 25 + 15
        Assert.Empty(result.AppliedRules); // No pricing rules applied
        Assert.Empty(result.PolicyViolations);
    }

    [Fact]
    public async Task CalculateFareAsync_WeekendSummerHighDemand_ShouldApplyMultipleRules()
    {
        // Arrange - Weekend summer flight with high demand
        var request = new FareCalculationRequest
        {
            FlightId = Guid.NewGuid(),
            FareClassId = Guid.NewGuid(),
            BaseFare = 500m,
            DepartureDate = new DateTime(2025, 7, 12), // Saturday in July
            BookingDate = new DateTime(2025, 6, 1), // Booked well in advance
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            Route = "JFK-LAX",
            PassengerCount = 1,
            CurrentLoadFactor = 0.9 // High demand
        };

        // Act
        var result = await _pricingService.CalculateFareAsync(request);

        // Assert
        Assert.True(result.Success);
        
        // Should apply weekend surcharge (15%) and summer multiplier (25%) and demand surcharge (30%)
        // Weekend: 500 * 1.15 = 575
        // Summer: 575 * 1.25 = 718.75
        // Demand: 718.75 * 1.3 = 934.375 ≈ 934.38
        Assert.True(result.FareBreakdown.AdjustedBaseFare > 900m);
        Assert.True(result.AppliedRules.Count >= 3); // Weekend, Summer, Demand
        
        // Check specific rules were applied
        Assert.Contains(result.AppliedRules, r => r.RuleType == "WeekendSurcharge");
        Assert.Contains(result.AppliedRules, r => r.RuleType == "SeasonalMultiplier");
        Assert.Contains(result.AppliedRules, r => r.RuleType == "DemandBased");
        
        Assert.NotNull(result.Explanation);
        Assert.NotEmpty(result.Explanation.Steps);
        Assert.NotEmpty(result.Explanation.KeyFactors);
    }

    [Fact]
    public async Task CalculateFareAsync_WithValidPromoCode_ShouldApplyDiscount()
    {
        // Arrange - Flight with promotional code
        var request = new FareCalculationRequest
        {
            FlightId = Guid.NewGuid(),
            FareClassId = Guid.NewGuid(),
            BaseFare = 500m,
            DepartureDate = new DateTime(2025, 5, 15), // Regular weekday
            BookingDate = new DateTime(2025, 5, 1), // Booked 2 weeks in advance
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            Route = "JFK-LAX",
            PassengerCount = 1,
            CurrentLoadFactor = 0.5,
            PromoCode = "SAVE20" // 20% discount
        };

        // Act
        var result = await _pricingService.CalculateFareAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(400m, result.FareBreakdown.AdjustedBaseFare); // 500 - (500 * 0.20)
        Assert.Equal(100m, result.FareBreakdown.TotalDiscount);
        Assert.True(result.FareBreakdown.HasDiscount);
        Assert.Contains(result.AppliedRules, r => r.RuleType == "PromotionalDiscount");
        Assert.Contains("saved", result.Explanation.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateFareAsync_InsufficientAdvancePurchase_ShouldFail()
    {
        // Arrange - Booking too close to departure
        var request = new FareCalculationRequest
        {
            FlightId = Guid.NewGuid(),
            FareClassId = Guid.NewGuid(),
            BaseFare = 500m,
            DepartureDate = new DateTime(2025, 1, 10), // Only 3 days advance
            BookingDate = new DateTime(2025, 1, 7),
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            Route = "JFK-LAX",
            PassengerCount = 1
        };

        // Act
        var result = await _pricingService.CalculateFareAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.PolicyViolations);
        Assert.Equal("ADV_PURCHASE_MIN", result.PolicyViolations[0].PolicyId);
        Assert.True(result.PolicyViolations[0].IsBlocking);
        Assert.Contains("7 days in advance", result.PolicyViolations[0].Description);
    }

    [Fact]
    public async Task CalculateFareAsync_ComplexScenario_ShouldHandleAllFactors()
    {
        // Arrange - Complex scenario: Weekend + Summer + High Demand + Promo Code
        var request = new FareCalculationRequest
        {
            FlightId = Guid.NewGuid(),
            FareClassId = Guid.NewGuid(),
            BaseFare = 1000m,
            DepartureDate = new DateTime(2025, 7, 12), // Saturday in July
            BookingDate = new DateTime(2025, 6, 1), // Booked well in advance
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            Route = "JFK-LAX",
            PassengerCount = 2,
            CurrentLoadFactor = 0.9, // High demand
            PromoCode = "SAVE20", // 20% discount
            RequestedExtras = new List<ExtraService>
            {
                new() { Code = "BAG", Name = "Extra Baggage", Type = ExtraServiceType.BaggageAllowance, Quantity = 1 }
            }
        };

        // Setup extra services
        _mockExtraServicesService.Setup(x => x.CalculateServiceChargesAsync(It.IsAny<List<ExtraService>>(), It.IsAny<FareCalculationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtraServiceCharge>
            {
                new() { Service = request.RequestedExtras[0], UnitPrice = 50m, TotalPrice = 50m }
            });

        // Act
        var result = await _pricingService.CalculateFareAsync(request);

        // Assert
        Assert.True(result.Success);
        
        // Should have multiple rules applied
        Assert.True(result.AppliedRules.Count >= 4); // Weekend, Summer, Demand, Promo
        
        // Should have comprehensive breakdown
        Assert.True(result.FareBreakdown.Components.Count >= 4);
        Assert.Equal(25m, result.FareBreakdown.TotalTaxes);
        Assert.Equal(15m, result.FareBreakdown.TotalFees);
        Assert.Equal(50m, result.FareBreakdown.TotalExtras);
        
        // Should have detailed explanation
        Assert.NotEmpty(result.Explanation.Steps);
        Assert.NotEmpty(result.Explanation.KeyFactors);
        Assert.NotEmpty(result.Explanation.Recommendations);
        
        // Should track calculation metadata
        Assert.NotEmpty(result.CalculationId);
        Assert.True(result.CalculationDuration.TotalMilliseconds > 0);
    }

    [Fact]
    public async Task ValidatePoliciesAsync_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            FlightId = Guid.NewGuid(),
            FareClassId = Guid.NewGuid(),
            DepartureDate = new DateTime(2025, 1, 21), // 14 days advance
            BookingDate = new DateTime(2025, 1, 7),
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            Route = "JFK-LAX",
            PassengerCount = 1
        };

        // Act
        var result = await _pricingService.ValidatePoliciesAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CalculateFareAsync_InvalidRequest_ShouldThrowArgumentException()
    {
        // Arrange - Invalid request with zero base fare
        var request = new FareCalculationRequest
        {
            FlightId = Guid.NewGuid(),
            FareClassId = Guid.NewGuid(),
            BaseFare = 0m, // Invalid
            DepartureDate = new DateTime(2025, 1, 21),
            BookingDate = new DateTime(2025, 1, 7),
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            Route = "JFK-LAX",
            PassengerCount = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _pricingService.CalculateFareAsync(request));
    }

    [Fact]
    public async Task CalculateFareAsync_RuleApplicationOrder_ShouldRespectPriority()
    {
        // Arrange - Scenario where rule order matters
        var request = new FareCalculationRequest
        {
            FlightId = Guid.NewGuid(),
            FareClassId = Guid.NewGuid(),
            BaseFare = 500m,
            DepartureDate = new DateTime(2025, 7, 12), // Saturday in July
            BookingDate = new DateTime(2025, 6, 1), // Booked well in advance
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            Route = "JFK-LAX",
            PassengerCount = 1,
            CurrentLoadFactor = 0.9,
            PromoCode = "SAVE20"
        };

        // Act
        var result = await _pricingService.CalculateFareAsync(request);

        // Assert
        Assert.True(result.Success);
        
        // Rules should be applied in priority order (lower priority number = higher priority)
        var orderedRules = result.AppliedRules.OrderBy(r => r.Priority).ToList();
        Assert.Equal(result.AppliedRules.Count, orderedRules.Count);
        
        // Verify the steps show the progression
        Assert.True(result.Explanation.Steps.Count >= 3);
        Assert.All(result.Explanation.Steps, step => Assert.True(step.Order > 0));
        
        // First step should start with base fare
        Assert.Equal(500m, result.Explanation.Steps.First().BeforeAmount);
    }
}

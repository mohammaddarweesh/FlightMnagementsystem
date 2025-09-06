using FlightBooking.Application.Pricing.Strategies;
using FlightBooking.Domain.Pricing;
using Xunit;

namespace FlightBooking.UnitTests.Pricing;

public class WeekendSurchargeStrategyTests
{
    private readonly WeekendSurchargeStrategy _strategy;
    private readonly WeekendSurchargeConfig _config;

    public WeekendSurchargeStrategyTests()
    {
        _config = new WeekendSurchargeConfig
        {
            SurchargePercentage = 15.0m,
            FlatSurcharge = 0m,
            HolidayMultiplier = 1.5m,
            PeakSeasonMultiplier = 1.2m,
            RoundTripWeekendMultiplier = 1.1m
        };
        _strategy = new WeekendSurchargeStrategy(_config);
    }

    [Fact]
    public void CanApply_SaturdayDeparture_ShouldReturnTrue()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 9, 6), // Saturday
            BaseFare = 500m
        };
        var context = new PricingContext { CurrentFare = 500m };

        // Act
        var canApply = _strategy.CanApply(request, context);

        // Assert
        Assert.True(canApply);
    }

    [Fact]
    public void CanApply_WeekdayDeparture_ShouldReturnFalse()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 9, 3), // Wednesday
            BaseFare = 500m
        };
        var context = new PricingContext { CurrentFare = 500m };

        // Act
        var canApply = _strategy.CanApply(request, context);

        // Assert
        Assert.False(canApply);
    }

    [Fact]
    public async Task ApplyAsync_SaturdayDeparture_ShouldApply15PercentSurcharge()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 9, 6), // Saturday
            BaseFare = 500m
        };
        var context = new PricingContext 
        { 
            CurrentFare = 500m,
            OriginalBaseFare = 500m
        };

        // Act
        var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(575m, result.AdjustedFare); // 500 + (500 * 0.15)
        Assert.Equal(75m, result.Adjustment);
        Assert.NotNull(result.AppliedRule);
        Assert.Equal("WEEKEND_SURCHARGE", result.AppliedRule.RuleId);
        Assert.Single(result.Components);
        Assert.Equal("Weekend Surcharge", result.Components[0].Name);
    }

    [Fact]
    public async Task ApplyAsync_SundayDepartureInSummer_ShouldApplyPeakSeasonMultiplier()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 7, 6), // Sunday in July (summer)
            BaseFare = 500m
        };
        var context = new PricingContext 
        { 
            CurrentFare = 500m,
            OriginalBaseFare = 500m
        };

        // Act
        var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        // Base surcharge: 500 * 0.15 = 75
        // Peak season multiplier: 75 * 1.2 = 90
        // Final fare: 500 + 90 = 590
        Assert.Equal(590m, result.AdjustedFare);
        Assert.Equal(90m, result.Adjustment);
    }

    [Fact]
    public async Task ApplyAsync_RoundTripBothWeekends_ShouldApplyRoundTripMultiplier()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            DepartureDate = new DateTime(2025, 9, 6), // Saturday
            IsRoundTrip = true,
            ReturnDate = new DateTime(2025, 9, 7), // Sunday
            BaseFare = 500m
        };
        var context = new PricingContext 
        { 
            CurrentFare = 500m,
            OriginalBaseFare = 500m
        };

        // Act
        var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        // Base surcharge: 500 * 0.15 = 75
        // Round trip weekend multiplier: 75 * 1.1 = 82.5
        // Final fare: 500 + 82.5 = 582.5
        Assert.Equal(582.5m, result.AdjustedFare);
        Assert.Equal(82.5m, result.Adjustment);
    }
}

public class SeasonalMultiplierStrategyTests
{
    private readonly SeasonalMultiplierStrategy _strategy;
    private readonly SeasonalMultiplierConfig _config;

    public SeasonalMultiplierStrategyTests()
    {
        _config = new SeasonalMultiplierConfig
        {
            WinterMultiplier = 1.3m,
            SpringMultiplier = 1.15m,
            SummerMultiplier = 1.25m,
            ThanksgivingMultiplier = 1.4m
        };
        _strategy = new SeasonalMultiplierStrategy(_config);
    }

    [Theory]
    [InlineData("2025-12-25", 1.3)] // Christmas - Winter
    [InlineData("2025-07-15", 1.25)] // Summer
    [InlineData("2025-11-28", 1.4)] // Thanksgiving
    [InlineData("2025-03-15", 1.15)] // Spring
    [InlineData("2025-05-15", 1.0)] // Regular season
    public async Task ApplyAsync_DifferentSeasons_ShouldApplyCorrectMultiplier(string dateString, decimal expectedMultiplier)
    {
        // Arrange
        var departureDate = DateTime.Parse(dateString);
        var request = new FareCalculationRequest
        {
            DepartureDate = departureDate,
            BaseFare = 500m
        };
        var context = new PricingContext 
        { 
            CurrentFare = 500m,
            OriginalBaseFare = 500m
        };

        // Act
        var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);

        // Assert
        if (expectedMultiplier == 1.0m)
        {
            // Should not apply for regular season
            Assert.False(_strategy.CanApply(request, context));
        }
        else
        {
            Assert.True(result.Success);
            var expectedFare = 500m * expectedMultiplier;
            Assert.Equal(expectedFare, result.AdjustedFare);
            Assert.Equal(expectedFare - 500m, result.Adjustment);
        }
    }
}

public class DemandBasedStrategyTests
{
    private readonly DemandBasedStrategy _strategy;
    private readonly DemandBasedConfig _config;

    public DemandBasedStrategyTests()
    {
        _config = new DemandBasedConfig
        {
            MinimumLoadFactorThreshold = 0.6,
            LoadFactorTiers = new List<LoadFactorTier>
            {
                new() { Name = "Standard", MinLoadFactor = 0.60, Multiplier = 1.05m },
                new() { Name = "Moderate", MinLoadFactor = 0.70, Multiplier = 1.15m },
                new() { Name = "High", MinLoadFactor = 0.85, Multiplier = 1.3m },
                new() { Name = "Very High", MinLoadFactor = 0.95, Multiplier = 1.5m }
            }
        };
        _strategy = new DemandBasedStrategy(_config);
    }

    [Theory]
    [InlineData(0.5, false, 0)] // Below threshold
    [InlineData(0.65, true, 1.05)] // Standard demand
    [InlineData(0.75, true, 1.15)] // Moderate demand
    [InlineData(0.90, true, 1.3)] // High demand
    [InlineData(0.98, true, 1.5)] // Very high demand
    public async Task ApplyAsync_DifferentLoadFactors_ShouldApplyCorrectMultiplier(double loadFactor, bool shouldApply, decimal expectedMultiplier)
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            CurrentLoadFactor = loadFactor,
            BaseFare = 500m,
            DepartureDate = DateTime.Today.AddDays(7)
        };
        var context = new PricingContext 
        { 
            CurrentFare = 500m,
            OriginalBaseFare = 500m
        };

        // Act
        var canApply = _strategy.CanApply(request, context);
        
        // Assert
        Assert.Equal(shouldApply, canApply);

        if (shouldApply)
        {
            var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);
            Assert.True(result.Success);
            var expectedFare = 500m * expectedMultiplier;
            Assert.Equal(expectedFare, result.AdjustedFare);
        }
    }

    [Fact]
    public async Task ApplyAsync_HighDemandLastMinute_ShouldIncludeLastMinuteExplanation()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            CurrentLoadFactor = 0.90, // High demand
            BaseFare = 500m,
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(3) // Last minute
        };
        var context = new PricingContext 
        { 
            CurrentFare = 500m,
            OriginalBaseFare = 500m
        };

        // Act
        var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Last-minute bookings", result.Explanation);
        Assert.Contains("high-demand flights", result.Explanation);
    }
}

public class PromotionalDiscountStrategyTests
{
    private readonly PromotionalDiscountStrategy _strategy;
    private readonly PromotionalDiscountConfig _config;

    public PromotionalDiscountStrategyTests()
    {
        _config = new PromotionalDiscountConfig
        {
            Promotions = new List<Promotion>
            {
                new()
                {
                    Code = "SAVE20",
                    Name = "20% Off Summer Sale",
                    Description = "Save 20% on summer travel",
                    Type = PromotionType.Percentage,
                    Value = 20m,
                    ValidFrom = DateTime.Today.AddDays(-30),
                    ValidTo = DateTime.Today.AddDays(30),
                    IsActive = true,
                    MinPurchaseAmount = 200m
                },
                new()
                {
                    Code = "FLAT50",
                    Name = "$50 Off",
                    Description = "Flat $50 discount",
                    Type = PromotionType.FixedAmount,
                    Value = 50m,
                    ValidFrom = DateTime.Today.AddDays(-10),
                    ValidTo = DateTime.Today.AddDays(10),
                    IsActive = true,
                    MaxDiscount = 50m
                }
            }
        };
        _strategy = new PromotionalDiscountStrategy(_config);
    }

    [Fact]
    public async Task ApplyAsync_ValidPercentagePromo_ShouldApplyCorrectDiscount()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            PromoCode = "SAVE20",
            BaseFare = 500m,
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(14)
        };
        var context = new PricingContext 
        { 
            CurrentFare = 500m,
            OriginalBaseFare = 500m
        };

        // Act
        var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(400m, result.AdjustedFare); // 500 - (500 * 0.20)
        Assert.Equal(-100m, result.Adjustment); // Negative because it's a discount
        Assert.Equal("PROMO_SAVE20", result.AppliedRule!.RuleId);
    }

    [Fact]
    public async Task ApplyAsync_ValidFixedAmountPromo_ShouldApplyCorrectDiscount()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            PromoCode = "FLAT50",
            BaseFare = 300m,
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(14)
        };
        var context = new PricingContext 
        { 
            CurrentFare = 300m,
            OriginalBaseFare = 300m
        };

        // Act
        var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(250m, result.AdjustedFare); // 300 - 50
        Assert.Equal(-50m, result.Adjustment);
    }

    [Fact]
    public async Task ApplyAsync_InvalidPromoCode_ShouldFail()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            PromoCode = "INVALID",
            BaseFare = 500m,
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(14)
        };
        var context = new PricingContext 
        { 
            CurrentFare = 500m,
            OriginalBaseFare = 500m
        };

        // Act
        var result = await _strategy.ApplyAsync(request, context, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid or expired promotional code", result.ErrorMessage);
    }

    [Fact]
    public async Task ApplyAsync_BelowMinimumPurchase_ShouldNotApply()
    {
        // Arrange
        var request = new FareCalculationRequest
        {
            PromoCode = "SAVE20", // Requires minimum $200
            BaseFare = 150m, // Below minimum
            BookingDate = DateTime.Today,
            DepartureDate = DateTime.Today.AddDays(14)
        };
        var context = new PricingContext 
        { 
            CurrentFare = 150m,
            OriginalBaseFare = 150m
        };

        // Act
        var canApply = _strategy.CanApply(request, context);

        // Assert
        Assert.False(canApply);
    }
}

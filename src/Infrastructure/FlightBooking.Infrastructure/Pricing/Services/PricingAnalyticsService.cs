using FlightBooking.Application.Pricing.Services;
using FlightBooking.Domain.Pricing;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Pricing.Services;

public class PricingAnalyticsService : IPricingAnalyticsService
{
    private readonly ILogger<PricingAnalyticsService> _logger;

    public PricingAnalyticsService(ILogger<PricingAnalyticsService> logger)
    {
        _logger = logger;
    }

    public Task<PricingEducation> GeneratePricingEducationAsync(FareCalculationRequest request, FareCalculationResult result, CancellationToken cancellationToken = default)
    {
        try
        {
            var education = new PricingEducation
            {
                Summary = GeneratePricingSummary(request, result),
                Factors = GeneratePricingFactors(result),
                SavingsTips = GenerateSavingsTips(request, result),
                Comparisons = GeneratePricingComparisons(request, result),
                Glossary = GeneratePricingGlossary()
            };

            return Task.FromResult(education);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pricing education");
            return Task.FromResult(new PricingEducation());
        }
    }

    public Task<List<SavingsTip>> GetSavingsTipsAsync(FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var tips = new List<SavingsTip>();

            // Advance booking tip
            var daysUntilDeparture = (request.DepartureDate - request.BookingDate).TotalDays;
            if (daysUntilDeparture < 21)
            {
                tips.Add(new SavingsTip
                {
                    Title = "Book Earlier for Better Rates",
                    Description = "Booking 21+ days in advance typically offers 10-20% savings",
                    PotentialSavings = request.BaseFare * 0.15m,
                    ActionRequired = "Consider flexible dates and book earlier",
                    Priority = 1
                });
            }

            // Weekend travel tip
            if (IsWeekend(request.DepartureDate))
            {
                tips.Add(new SavingsTip
                {
                    Title = "Avoid Weekend Travel",
                    Description = "Flying on weekdays can save 15-25% compared to weekends",
                    PotentialSavings = request.BaseFare * 0.20m,
                    ActionRequired = "Consider departing on Tuesday, Wednesday, or Thursday",
                    Priority = 2
                });
            }

            // Seasonal tip
            if (IsPeakSeason(request.DepartureDate))
            {
                tips.Add(new SavingsTip
                {
                    Title = "Travel During Off-Peak Season",
                    Description = "Avoiding peak travel seasons can result in significant savings",
                    PotentialSavings = request.BaseFare * 0.30m,
                    ActionRequired = "Consider traveling in shoulder seasons",
                    Priority = 3
                });
            }

            // Flexible dates tip
            if (!request.IsFlexibleDates)
            {
                tips.Add(new SavingsTip
                {
                    Title = "Use Flexible Dates",
                    Description = "Being flexible with your travel dates can unlock better deals",
                    PotentialSavings = request.BaseFare * 0.12m,
                    ActionRequired = "Search with flexible date options",
                    Priority = 4
                });
            }

            return Task.FromResult(tips.OrderBy(t => t.Priority).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating savings tips");
            return Task.FromResult(new List<SavingsTip>());
        }
    }

    public Task<List<PricingComparison>> GetPricingComparisonsAsync(FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var comparisons = new List<PricingComparison>();

            // Weekday vs weekend comparison
            if (IsWeekend(request.DepartureDate))
            {
                var weekdayPrice = request.BaseFare * 0.85m; // 15% less
                comparisons.Add(new PricingComparison
                {
                    Scenario = "Weekday Departure",
                    Price = weekdayPrice,
                    Difference = weekdayPrice - request.BaseFare,
                    Description = "Flying on a weekday instead of weekend"
                });
            }

            // Earlier booking comparison
            var earlierBookingPrice = request.BaseFare * 0.88m; // 12% less
            comparisons.Add(new PricingComparison
            {
                Scenario = "30 Days Earlier",
                Price = earlierBookingPrice,
                Difference = earlierBookingPrice - request.BaseFare,
                Description = "If booked 30 days in advance"
            });

            // Off-peak season comparison
            if (IsPeakSeason(request.DepartureDate))
            {
                var offPeakPrice = request.BaseFare * 0.75m; // 25% less
                comparisons.Add(new PricingComparison
                {
                    Scenario = "Off-Peak Season",
                    Price = offPeakPrice,
                    Difference = offPeakPrice - request.BaseFare,
                    Description = "Traveling during off-peak season"
                });
            }

            return Task.FromResult(comparisons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pricing comparisons");
            return Task.FromResult(new List<PricingComparison>());
        }
    }

    public Task<List<PricingScenario>> SimulateScenariosAsync(FareCalculationRequest baseRequest, List<ScenarioParameter> scenarios, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<PricingScenario>();

            foreach (var scenario in scenarios)
            {
                var modifiedRequest = CloneRequest(baseRequest);
                ApplyScenarioParameter(modifiedRequest, scenario);

                // Simulate the pricing calculation (simplified)
                var simulatedResult = SimulatePricing(modifiedRequest);

                results.Add(new PricingScenario
                {
                    Name = scenario.ParameterName,
                    Description = scenario.Description,
                    ModifiedRequest = modifiedRequest,
                    Result = simulatedResult,
                    PriceDifference = simulatedResult.FareBreakdown.GrandTotal - baseRequest.BaseFare,
                    Recommendation = GenerateScenarioRecommendation(scenario, simulatedResult)
                });
            }

            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating pricing scenarios");
            return Task.FromResult(new List<PricingScenario>());
        }
    }

    private string GeneratePricingSummary(FareCalculationRequest request, FareCalculationResult result)
    {
        if (!result.Success)
        {
            return "Pricing calculation was not successful due to policy violations or errors.";
        }

        var totalAdjustment = result.FareBreakdown.AdjustedBaseFare - result.FareBreakdown.BaseFare;
        var adjustmentPercentage = result.FareBreakdown.BaseFare > 0 ? 
            (totalAdjustment / result.FareBreakdown.BaseFare) * 100 : 0;

        if (Math.Abs(adjustmentPercentage) < 1)
        {
            return "Your fare is based on standard pricing with minimal adjustments.";
        }
        else if (adjustmentPercentage > 0)
        {
            return $"Your fare includes a {adjustmentPercentage:F1}% adjustment ({totalAdjustment:C}) due to demand, timing, and seasonal factors.";
        }
        else
        {
            return $"You saved {Math.Abs(adjustmentPercentage):F1}% ({Math.Abs(totalAdjustment):C}) through applicable discounts and promotions.";
        }
    }

    private List<PricingFactor> GeneratePricingFactors(FareCalculationResult result)
    {
        var factors = new List<PricingFactor>();

        foreach (var rule in result.AppliedRules)
        {
            factors.Add(new PricingFactor
            {
                Name = rule.RuleName,
                Description = rule.Description,
                Impact = rule.Impact,
                ImpactType = rule.ImpactType,
                Category = GetFactorCategory(rule.RuleType)
            });
        }

        return factors.OrderByDescending(f => Math.Abs(f.Impact)).ToList();
    }

    private List<SavingsTip> GenerateSavingsTips(FareCalculationRequest request, FareCalculationResult result)
    {
        // This would call the main GetSavingsTipsAsync method
        return GetSavingsTipsAsync(request).Result;
    }

    private List<PricingComparison> GeneratePricingComparisons(FareCalculationRequest request, FareCalculationResult result)
    {
        // This would call the main GetPricingComparisonsAsync method
        return GetPricingComparisonsAsync(request).Result;
    }

    private Dictionary<string, string> GeneratePricingGlossary()
    {
        return new Dictionary<string, string>
        {
            ["Base Fare"] = "The basic cost of your flight before taxes, fees, and adjustments",
            ["Weekend Surcharge"] = "Additional fee applied to flights departing on weekends due to higher demand",
            ["Seasonal Adjustment"] = "Price modification based on travel season and demand patterns",
            ["Demand-Based Pricing"] = "Dynamic pricing that adjusts based on current flight capacity and demand",
            ["Promotional Discount"] = "Savings applied through valid promotional codes or special offers",
            ["Airport Tax"] = "Government-imposed fee for using airport facilities",
            ["Security Fee"] = "Mandatory charge for aviation security services",
            ["Fuel Surcharge"] = "Additional fee to offset fluctuating fuel costs",
            ["Booking Fee"] = "Service charge for processing your reservation",
            ["Load Factor"] = "Percentage of seats already booked on the flight"
        };
    }

    private string GetFactorCategory(string ruleType)
    {
        return ruleType switch
        {
            "WeekendSurcharge" => "Timing",
            "SeasonalMultiplier" => "Seasonal",
            "DemandBased" => "Demand",
            "PromotionalDiscount" => "Promotion",
            "AdvancePurchase" => "Booking Window",
            _ => "Other"
        };
    }

    private bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    private bool IsPeakSeason(DateTime date)
    {
        // Summer (June-August) and Winter holidays (December-January)
        return (date.Month >= 6 && date.Month <= 8) || date.Month == 12 || date.Month == 1;
    }

    private FareCalculationRequest CloneRequest(FareCalculationRequest original)
    {
        return new FareCalculationRequest
        {
            FlightId = original.FlightId,
            FareClassId = original.FareClassId,
            BaseFare = original.BaseFare,
            PassengerCount = original.PassengerCount,
            BookingDate = original.BookingDate,
            DepartureDate = original.DepartureDate,
            DepartureAirport = original.DepartureAirport,
            ArrivalAirport = original.ArrivalAirport,
            Route = original.Route,
            IsRoundTrip = original.IsRoundTrip,
            ReturnDate = original.ReturnDate,
            PromoCode = original.PromoCode,
            PassengerTypes = new List<string>(original.PassengerTypes),
            RequestedExtras = new List<ExtraService>(original.RequestedExtras),
            CurrentLoadFactor = original.CurrentLoadFactor,
            CorporateCode = original.CorporateCode,
            IsFlexibleDates = original.IsFlexibleDates,
            Currency = original.Currency
        };
    }

    private void ApplyScenarioParameter(FareCalculationRequest request, ScenarioParameter scenario)
    {
        switch (scenario.ParameterName.ToLower())
        {
            case "departure_date":
                if (scenario.Value is DateTime newDate)
                    request.DepartureDate = newDate;
                break;
            case "booking_date":
                if (scenario.Value is DateTime bookingDate)
                    request.BookingDate = bookingDate;
                break;
            case "passenger_count":
                if (scenario.Value is int passengerCount)
                    request.PassengerCount = passengerCount;
                break;
            case "load_factor":
                if (scenario.Value is double loadFactor)
                    request.CurrentLoadFactor = loadFactor;
                break;
        }
    }

    private FareCalculationResult SimulatePricing(FareCalculationRequest request)
    {
        // Simplified simulation - in real implementation, this would use the actual pricing service
        var adjustedFare = request.BaseFare;

        // Apply basic adjustments for simulation
        if (IsWeekend(request.DepartureDate))
            adjustedFare *= 1.15m;

        if (IsPeakSeason(request.DepartureDate))
            adjustedFare *= 1.25m;

        return new FareCalculationResult
        {
            Success = true,
            FareBreakdown = new FareBreakdown
            {
                BaseFare = request.BaseFare,
                AdjustedBaseFare = adjustedFare,
                GrandTotal = adjustedFare + 40m, // Add estimated taxes/fees
                Currency = request.Currency
            }
        };
    }

    private string GenerateScenarioRecommendation(ScenarioParameter scenario, FareCalculationResult result)
    {
        var savings = result.FareBreakdown.GrandTotal;
        
        return scenario.ParameterName.ToLower() switch
        {
            "departure_date" => savings < 0 ? "Consider this alternative date for savings" : "Current date offers better value",
            "booking_date" => "Booking earlier typically results in better rates",
            "passenger_count" => "Group bookings may qualify for additional discounts",
            "load_factor" => "Lower demand flights offer better pricing",
            _ => "Compare this scenario with your original selection"
        };
    }
}

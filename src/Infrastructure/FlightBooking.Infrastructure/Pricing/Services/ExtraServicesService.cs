using FlightBooking.Application.Pricing.Services;
using FlightBooking.Domain.Pricing;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Pricing.Services;

public class ExtraServicesService : IExtraServicesService
{
    private readonly ILogger<ExtraServicesService> _logger;

    public ExtraServicesService(ILogger<ExtraServicesService> logger)
    {
        _logger = logger;
    }

    public Task<List<ExtraServiceCharge>> CalculateServiceChargesAsync(List<ExtraService> services, FareCalculationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var charges = new List<ExtraServiceCharge>();

            foreach (var service in services)
            {
                var unitPrice = GetServicePrice(service, request);
                var totalPrice = unitPrice * service.Quantity;

                charges.Add(new ExtraServiceCharge
                {
                    Service = service,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    IsRefundable = IsServiceRefundable(service),
                    Terms = GetServiceTerms(service)
                });
            }

            _logger.LogDebug("Calculated charges for {ServiceCount} extra services, total: {TotalAmount:C}", 
                services.Count, charges.Sum(c => c.TotalPrice));

            return Task.FromResult(charges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating extra service charges");
            return Task.FromResult(new List<ExtraServiceCharge>());
        }
    }

    public Task<List<ExtraService>> GetAvailableServicesAsync(Guid flightId, Guid fareClassId, CancellationToken cancellationToken = default)
    {
        try
        {
            var services = new List<ExtraService>
            {
                new() { Code = "BAG_EXTRA", Name = "Extra Baggage", Description = "Additional checked baggage allowance", Type = ExtraServiceType.BaggageAllowance },
                new() { Code = "SEAT_SELECT", Name = "Seat Selection", Description = "Choose your preferred seat", Type = ExtraServiceType.SeatSelection },
                new() { Code = "MEAL_UPGRADE", Name = "Meal Upgrade", Description = "Premium meal service", Type = ExtraServiceType.MealUpgrade },
                new() { Code = "PRIORITY_BOARD", Name = "Priority Boarding", Description = "Board the aircraft first", Type = ExtraServiceType.PriorityBoarding },
                new() { Code = "LOUNGE_ACCESS", Name = "Lounge Access", Description = "Access to airport lounge", Type = ExtraServiceType.LoungeAccess },
                new() { Code = "FAST_TRACK", Name = "Fast Track Security", Description = "Skip security lines", Type = ExtraServiceType.FastTrackSecurity },
                new() { Code = "INSURANCE", Name = "Travel Insurance", Description = "Comprehensive travel protection", Type = ExtraServiceType.Insurance }
            };

            return Task.FromResult(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available services for flight {FlightId}", flightId);
            return Task.FromResult(new List<ExtraService>());
        }
    }

    public Task<Dictionary<string, decimal>> GetBundleDiscountsAsync(List<ExtraService> services, CancellationToken cancellationToken = default)
    {
        try
        {
            var discounts = new Dictionary<string, decimal>();

            // Travel bundle discount
            if (HasTravelBundle(services))
            {
                discounts["travel_bundle"] = 25m; // $25 off for travel bundle
            }

            // Comfort bundle discount
            if (HasComfortBundle(services))
            {
                discounts["comfort_bundle"] = 15m; // $15 off for comfort bundle
            }

            // Premium bundle discount
            if (HasPremiumBundle(services))
            {
                discounts["premium_bundle"] = 50m; // $50 off for premium bundle
            }

            return Task.FromResult(discounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bundle discounts");
            return Task.FromResult(new Dictionary<string, decimal>());
        }
    }

    private decimal GetServicePrice(ExtraService service, FareCalculationRequest request)
    {
        var basePrice = service.Type switch
        {
            ExtraServiceType.BaggageAllowance => 75m,
            ExtraServiceType.SeatSelection => GetSeatSelectionPrice(request),
            ExtraServiceType.MealUpgrade => 35m,
            ExtraServiceType.PriorityBoarding => 25m,
            ExtraServiceType.LoungeAccess => 65m,
            ExtraServiceType.FastTrackSecurity => 15m,
            ExtraServiceType.Insurance => CalculateInsurancePrice(request.BaseFare),
            ExtraServiceType.SpecialAssistance => 0m, // Free service
            ExtraServiceType.PetTransport => 150m,
            ExtraServiceType.UnaccompaniedMinor => 100m,
            _ => 0m
        };

        // Apply route-based adjustments
        if (IsInternationalRoute(request.Route))
        {
            basePrice *= 1.2m; // 20% increase for international routes
        }

        return Math.Round(basePrice, 2);
    }

    private decimal GetSeatSelectionPrice(FareCalculationRequest request)
    {
        // Seat selection pricing varies by route and fare class
        var basePrice = 20m;

        if (IsInternationalRoute(request.Route))
        {
            basePrice = 35m;
        }

        // Premium locations cost more
        return basePrice;
    }

    private decimal CalculateInsurancePrice(decimal baseFare)
    {
        // Insurance is typically 5-8% of base fare
        var rate = 0.06m; // 6%
        var minPrice = 25m;
        var maxPrice = 200m;

        var calculatedPrice = baseFare * rate;
        return Math.Max(minPrice, Math.Min(maxPrice, calculatedPrice));
    }

    private bool IsServiceRefundable(ExtraService service)
    {
        return service.Type switch
        {
            ExtraServiceType.Insurance => true,
            ExtraServiceType.BaggageAllowance => false,
            ExtraServiceType.SeatSelection => true,
            ExtraServiceType.MealUpgrade => false,
            ExtraServiceType.PriorityBoarding => false,
            ExtraServiceType.LoungeAccess => true,
            ExtraServiceType.FastTrackSecurity => false,
            _ => false
        };
    }

    private string GetServiceTerms(ExtraService service)
    {
        return service.Type switch
        {
            ExtraServiceType.Insurance => "Coverage subject to policy terms and conditions. Claims must be filed within 30 days.",
            ExtraServiceType.BaggageAllowance => "Additional baggage must not exceed weight and size restrictions.",
            ExtraServiceType.SeatSelection => "Seat assignment subject to aircraft configuration. Refundable up to 24 hours before departure.",
            ExtraServiceType.MealUpgrade => "Special dietary requirements must be requested 48 hours in advance.",
            ExtraServiceType.LoungeAccess => "Access valid for 3 hours. Guest passes available for additional fee.",
            ExtraServiceType.FastTrackSecurity => "Service availability subject to airport facilities.",
            _ => "Standard terms and conditions apply."
        };
    }

    private bool HasTravelBundle(List<ExtraService> services)
    {
        var bundleServices = new[] { "BAG_EXTRA", "INSURANCE", "SEAT_SELECT" };
        return bundleServices.All(code => services.Any(s => s.Code == code));
    }

    private bool HasComfortBundle(List<ExtraService> services)
    {
        var bundleServices = new[] { "SEAT_SELECT", "MEAL_UPGRADE", "PRIORITY_BOARD" };
        return bundleServices.All(code => services.Any(s => s.Code == code));
    }

    private bool HasPremiumBundle(List<ExtraService> services)
    {
        var bundleServices = new[] { "LOUNGE_ACCESS", "FAST_TRACK", "PRIORITY_BOARD", "MEAL_UPGRADE" };
        return bundleServices.All(code => services.Any(s => s.Code == code));
    }

    private bool IsInternationalRoute(string route)
    {
        var internationalRoutes = new[] { "JFK-LHR", "LAX-NRT", "SFO-CDG", "NYC-LON", "LAX-NRT" };
        return internationalRoutes.Contains(route);
    }
}

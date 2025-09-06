using FlightBooking.Application.Pricing.Services;
using FlightBooking.Domain.Pricing;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Pricing.Services;

public class TaxCalculationService : ITaxCalculationService
{
    private readonly ILogger<TaxCalculationService> _logger;

    public TaxCalculationService(ILogger<TaxCalculationService> logger)
    {
        _logger = logger;
    }

    public Task<List<Tax>> CalculateTaxesAsync(string route, string fareClass, decimal baseFare, CancellationToken cancellationToken = default)
    {
        try
        {
            var taxes = new List<Tax>();

            // Airport taxes
            taxes.Add(new Tax
            {
                Code = "APT",
                Name = "Airport Tax",
                Description = "Airport facility usage tax",
                Amount = CalculateAirportTax(route, baseFare),
                Rate = 0,
                Type = TaxType.AirportTax,
                Authority = "Airport Authority",
                IsRefundable = false
            });

            // Security tax
            taxes.Add(new Tax
            {
                Code = "SEC",
                Name = "Security Tax",
                Description = "Transportation security tax",
                Amount = CalculateSecurityTax(route),
                Rate = 0,
                Type = TaxType.SecurityTax,
                Authority = "Transportation Security Administration",
                IsRefundable = false
            });

            // Government taxes
            if (IsInternationalRoute(route))
            {
                taxes.Add(new Tax
                {
                    Code = "INTL",
                    Name = "International Tax",
                    Description = "International departure tax",
                    Amount = CalculateInternationalTax(route, baseFare),
                    Rate = 0.05m,
                    Type = TaxType.InternationalTax,
                    Authority = "Customs and Border Protection",
                    IsRefundable = false
                });
            }
            else
            {
                taxes.Add(new Tax
                {
                    Code = "DOM",
                    Name = "Domestic Tax",
                    Description = "Domestic travel tax",
                    Amount = CalculateDomesticTax(baseFare),
                    Rate = 0.075m,
                    Type = TaxType.DomesticTax,
                    Authority = "Federal Aviation Administration",
                    IsRefundable = false
                });
            }

            // Fuel surcharge
            taxes.Add(new Tax
            {
                Code = "FUEL",
                Name = "Fuel Surcharge",
                Description = "Fuel cost recovery surcharge",
                Amount = CalculateFuelSurcharge(route, baseFare),
                Rate = 0,
                Type = TaxType.FuelSurcharge,
                Authority = "Airline",
                IsRefundable = false
            });

            _logger.LogDebug("Calculated {TaxCount} taxes for route {Route}, total amount: {TotalAmount:C}", 
                taxes.Count, route, taxes.Sum(t => t.Amount));

            return Task.FromResult(taxes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating taxes for route {Route}", route);
            return Task.FromResult(new List<Tax>());
        }
    }

    public Task<List<Fee>> CalculateFeesAsync(string route, string fareClass, decimal baseFare, CancellationToken cancellationToken = default)
    {
        try
        {
            var fees = new List<Fee>();

            // Booking fee
            fees.Add(new Fee
            {
                Code = "BOOK",
                Name = "Booking Fee",
                Description = "Online booking processing fee",
                Amount = CalculateBookingFee(fareClass),
                Type = FeeType.BookingFee,
                IsOptional = false,
                IsRefundable = false
            });

            // Service fee
            fees.Add(new Fee
            {
                Code = "SVC",
                Name = "Service Fee",
                Description = "Customer service fee",
                Amount = CalculateServiceFee(fareClass),
                Type = FeeType.ServiceFee,
                IsOptional = false,
                IsRefundable = false
            });

            // Processing fee for premium fare classes
            if (IsPremiumFareClass(fareClass))
            {
                fees.Add(new Fee
                {
                    Code = "PROC",
                    Name = "Processing Fee",
                    Description = "Premium service processing fee",
                    Amount = 25m,
                    Type = FeeType.ProcessingFee,
                    IsOptional = false,
                    IsRefundable = true,
                    WaiverConditions = "Waived for elite members"
                });
            }

            _logger.LogDebug("Calculated {FeeCount} fees for route {Route} fare class {FareClass}, total amount: {TotalAmount:C}", 
                fees.Count, route, fareClass, fees.Sum(f => f.Amount));

            return Task.FromResult(fees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating fees for route {Route} fare class {FareClass}", route, fareClass);
            return Task.FromResult(new List<Fee>());
        }
    }

    private decimal CalculateAirportTax(string route, decimal baseFare)
    {
        // Different airports have different tax rates
        var airports = route.Split('-');
        var departureAirport = airports.Length > 0 ? airports[0] : "";
        var arrivalAirport = airports.Length > 1 ? airports[1] : "";

        var baseTax = 15m;

        // Major hub airports have higher taxes
        var majorHubs = new[] { "JFK", "LAX", "ORD", "ATL", "DFW", "LHR", "CDG", "NRT" };
        if (majorHubs.Contains(departureAirport) || majorHubs.Contains(arrivalAirport))
        {
            baseTax += 10m;
        }

        return baseTax;
    }

    private decimal CalculateSecurityTax(string route)
    {
        // Fixed security tax per segment
        return IsInternationalRoute(route) ? 5.60m : 11.20m;
    }

    private decimal CalculateInternationalTax(string route, decimal baseFare)
    {
        // International tax as percentage of base fare
        return Math.Round(baseFare * 0.05m, 2);
    }

    private decimal CalculateDomesticTax(decimal baseFare)
    {
        // Domestic tax as percentage of base fare
        return Math.Round(baseFare * 0.075m, 2);
    }

    private decimal CalculateFuelSurcharge(string route, decimal baseFare)
    {
        // Fuel surcharge based on route distance (simplified)
        var baseCharge = IsInternationalRoute(route) ? 50m : 25m;
        
        // Add percentage of base fare
        var percentageCharge = baseFare * 0.02m;
        
        return Math.Round(baseCharge + percentageCharge, 2);
    }

    private decimal CalculateBookingFee(string fareClass)
    {
        return fareClass.ToUpper() switch
        {
            "FIRST" => 0m, // No booking fee for first class
            "BUSINESS" => 5m,
            "PREMIUM_ECONOMY" => 10m,
            "ECONOMY" => 15m,
            _ => 15m
        };
    }

    private decimal CalculateServiceFee(string fareClass)
    {
        return fareClass.ToUpper() switch
        {
            "FIRST" => 0m, // No service fee for first class
            "BUSINESS" => 0m, // No service fee for business class
            "PREMIUM_ECONOMY" => 5m,
            "ECONOMY" => 10m,
            _ => 10m
        };
    }

    private bool IsInternationalRoute(string route)
    {
        // Simplified logic - in real implementation, this would check against airport/country data
        var internationalRoutes = new[] { "JFK-LHR", "LAX-NRT", "SFO-CDG", "NYC-LON", "LAX-NRT" };
        return internationalRoutes.Contains(route);
    }

    private bool IsPremiumFareClass(string fareClass)
    {
        var premiumClasses = new[] { "FIRST", "BUSINESS", "PREMIUM_ECONOMY" };
        return premiumClasses.Contains(fareClass.ToUpper());
    }
}

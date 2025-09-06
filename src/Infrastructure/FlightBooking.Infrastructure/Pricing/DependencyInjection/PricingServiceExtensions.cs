using FlightBooking.Application.Pricing.Services;
using FlightBooking.Application.Pricing.Strategies;
using FlightBooking.Application.Pricing.Validators;
using FlightBooking.Infrastructure.Pricing.Configuration;
using FlightBooking.Infrastructure.Pricing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlightBooking.Infrastructure.Pricing.DependencyInjection;

public static class PricingServiceExtensions
{
    public static IServiceCollection AddPricingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Core pricing service
        services.AddScoped<IPricingService, PricingService>();
        
        // Configuration service
        services.AddScoped<IPricingConfigurationService, PricingConfigurationService>();

        // Pricing strategies
        services.AddPricingStrategies(configuration);
        
        // Policy validators
        services.AddPolicyValidators(configuration);
        
        // Supporting services
        services.AddSupportingPricingServices();
        
        // Configuration options
        services.AddPricingConfiguration(configuration);

        return services;
    }

    private static IServiceCollection AddPricingStrategies(this IServiceCollection services, IConfiguration configuration)
    {
        // Weekend surcharge strategy
        services.Configure<WeekendSurchargeConfig>(configuration.GetSection("Pricing:WeekendSurcharge"));
        services.AddScoped<IPricingStrategy>(provider =>
        {
            var config = new WeekendSurchargeConfig();
            configuration.GetSection("Pricing:WeekendSurcharge").Bind(config);
            return new WeekendSurchargeStrategy(config);
        });

        // Seasonal multiplier strategy
        services.Configure<SeasonalMultiplierConfig>(configuration.GetSection("Pricing:SeasonalMultiplier"));
        services.AddScoped<IPricingStrategy>(provider =>
        {
            var config = new SeasonalMultiplierConfig();
            configuration.GetSection("Pricing:SeasonalMultiplier").Bind(config);
            return new SeasonalMultiplierStrategy(config);
        });

        // Demand-based strategy
        services.Configure<DemandBasedConfig>(configuration.GetSection("Pricing:DemandBased"));
        services.AddScoped<IPricingStrategy>(provider =>
        {
            var config = new DemandBasedConfig();
            configuration.GetSection("Pricing:DemandBased").Bind(config);
            return new DemandBasedStrategy(config);
        });

        // Promotional discount strategy
        services.Configure<PromotionalDiscountConfig>(configuration.GetSection("Pricing:PromotionalDiscount"));
        services.AddScoped<IPricingStrategy>(provider =>
        {
            var config = new PromotionalDiscountConfig();
            configuration.GetSection("Pricing:PromotionalDiscount").Bind(config);
            return new PromotionalDiscountStrategy(config);
        });

        return services;
    }

    private static IServiceCollection AddPolicyValidators(this IServiceCollection services, IConfiguration configuration)
    {
        // Advance purchase policy validator
        services.Configure<AdvancePurchasePolicyConfig>(configuration.GetSection("Pricing:Policies:AdvancePurchase"));
        services.AddScoped<IPolicyValidator>(provider =>
        {
            var config = new AdvancePurchasePolicyConfig();
            configuration.GetSection("Pricing:Policies:AdvancePurchase").Bind(config);
            return new AdvancePurchasePolicyValidator(config);
        });

        // Blackout date policy validator
        services.Configure<BlackoutDatePolicyConfig>(configuration.GetSection("Pricing:Policies:BlackoutDates"));
        services.AddScoped<IPolicyValidator>(provider =>
        {
            var config = new BlackoutDatePolicyConfig();
            configuration.GetSection("Pricing:Policies:BlackoutDates").Bind(config);
            return new BlackoutDatePolicyValidator(config);
        });

        // Route restriction policy validator
        services.Configure<RouteRestrictionPolicyConfig>(configuration.GetSection("Pricing:Policies:RouteRestrictions"));
        services.AddScoped<IPolicyValidator>(provider =>
        {
            var config = new RouteRestrictionPolicyConfig();
            configuration.GetSection("Pricing:Policies:RouteRestrictions").Bind(config);
            return new RouteRestrictionPolicyValidator(config);
        });

        return services;
    }

    private static IServiceCollection AddSupportingPricingServices(this IServiceCollection services)
    {
        // Tax calculation service
        services.AddScoped<ITaxCalculationService, TaxCalculationService>();
        
        // Extra services service
        services.AddScoped<IExtraServicesService, ExtraServicesService>();
        
        // Promotion service
        services.AddScoped<IPromotionService, PromotionService>();
        
        // Analytics service
        services.AddScoped<IPricingAnalyticsService, PricingAnalyticsService>();

        return services;
    }

    private static IServiceCollection AddPricingConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PricingOptions>(configuration.GetSection("Pricing"));
        return services;
    }

    public static IServiceCollection AddPricingHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<PricingServiceHealthCheck>("pricing_service")
            .AddCheck<PricingConfigurationHealthCheck>("pricing_configuration");

        return services;
    }
}

public class PricingOptions
{
    public bool EnableDynamicPricing { get; set; } = true;
    public bool EnablePromotionalDiscounts { get; set; } = true;
    public bool EnableSeasonalAdjustments { get; set; } = true;
    public bool EnableWeekendSurcharges { get; set; } = true;
    public bool EnableDemandBasedPricing { get; set; } = true;
    public bool EnablePolicyValidation { get; set; } = true;
    public int MaxConcurrentCalculations { get; set; } = 100;
    public TimeSpan CalculationTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public string DefaultCurrency { get; set; } = "USD";
    public bool EnablePricingAnalytics { get; set; } = true;
    public bool EnablePricingEducation { get; set; } = true;
}

public class PricingServiceHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IPricingService _pricingService;

    public PricingServiceHealthCheck(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a simple pricing calculation to verify service health
            var testRequest = new FlightBooking.Domain.Pricing.FareCalculationRequest
            {
                FlightId = Guid.NewGuid(),
                FareClassId = Guid.NewGuid(),
                BaseFare = 100m,
                DepartureDate = DateTime.Today.AddDays(30),
                BookingDate = DateTime.Today,
                DepartureAirport = "TEST",
                ArrivalAirport = "TEST",
                Route = "TEST-TEST",
                PassengerCount = 1
            };

            var result = await _pricingService.CalculateFareAsync(testRequest, cancellationToken);
            
            return result.Success
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Pricing service is operational")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"Pricing service returned error: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Pricing service is unhealthy", ex);
        }
    }
}

public class PricingConfigurationHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IPricingConfigurationService _configurationService;

    public PricingConfigurationHealthCheck(IPricingConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rules = await _configurationService.GetActivePricingRulesAsync(cancellationToken);
            var policies = await _configurationService.GetActivePoliciesAsync(cancellationToken);
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                $"Pricing configuration loaded: {rules.Count} rules, {policies.Count} policies");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Pricing configuration is unhealthy", ex);
        }
    }
}

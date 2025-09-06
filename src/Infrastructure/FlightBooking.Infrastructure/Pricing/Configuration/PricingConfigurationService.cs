using FlightBooking.Application.Pricing.Services;
using FlightBooking.Domain.Pricing;
using FlightBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Pricing.Configuration;

public class PricingConfigurationService : IPricingConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PricingConfigurationService> _logger;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    public PricingConfigurationService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<PricingConfigurationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<PricingRule>> GetActivePricingRulesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "pricing_rules_active";
        
        if (_cache.TryGetValue(cacheKey, out List<PricingRule>? cachedRules))
        {
            return cachedRules!;
        }

        try
        {
            var rules = await _context.Set<PricingRule>()
                .Where(r => r.IsActive && 
                           r.EffectiveFrom <= DateTime.UtcNow &&
                           (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
                .OrderBy(r => r.Priority)
                .ToListAsync(cancellationToken);

            _cache.Set(cacheKey, rules, _cacheExpiry);
            _logger.LogDebug("Loaded {RuleCount} active pricing rules", rules.Count);

            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active pricing rules");
            return new List<PricingRule>();
        }
    }

    public async Task<List<PricingPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "pricing_policies_active";
        
        if (_cache.TryGetValue(cacheKey, out List<PricingPolicy>? cachedPolicies))
        {
            return cachedPolicies!;
        }

        try
        {
            var policies = await _context.Set<PricingPolicy>()
                .Where(p => p.IsActive && 
                           p.EffectiveFrom <= DateTime.UtcNow &&
                           (p.EffectiveTo == null || p.EffectiveTo >= DateTime.UtcNow))
                .ToListAsync(cancellationToken);

            _cache.Set(cacheKey, policies, _cacheExpiry);
            _logger.LogDebug("Loaded {PolicyCount} active pricing policies", policies.Count);

            return policies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active pricing policies");
            return new List<PricingPolicy>();
        }
    }

    public async Task<PricingConfiguration> GetPricingConfigurationAsync(string route, string fareClass, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"pricing_config_{route}_{fareClass}";
        
        if (_cache.TryGetValue(cacheKey, out PricingConfiguration? cachedConfig))
        {
            return cachedConfig!;
        }

        try
        {
            var allRules = await GetActivePricingRulesAsync(cancellationToken);
            var allPolicies = await GetActivePoliciesAsync(cancellationToken);

            // Filter rules applicable to this route and fare class
            var applicableRules = allRules.Where(r => 
                (r.ApplicableRoutes.Count == 0 || r.ApplicableRoutes.Contains(route)) &&
                (r.ApplicableFareClasses.Count == 0 || r.ApplicableFareClasses.Contains(fareClass)))
                .ToList();

            // Filter policies applicable to this route and fare class
            var applicablePolicies = allPolicies.Where(p => 
                (p.ApplicableRoutes.Count == 0 || p.ApplicableRoutes.Contains(route)) &&
                (p.ApplicableFareClasses.Count == 0 || p.ApplicableFareClasses.Contains(fareClass)))
                .ToList();

            var configuration = new PricingConfiguration
            {
                Rules = applicableRules,
                Policies = applicablePolicies,
                LastUpdated = DateTime.UtcNow,
                Version = GenerateConfigurationVersion(applicableRules, applicablePolicies),
                Settings = await GetPricingSettingsAsync(route, fareClass, cancellationToken)
            };

            _cache.Set(cacheKey, configuration, _cacheExpiry);
            _logger.LogDebug("Generated pricing configuration for route {Route} fare class {FareClass} with {RuleCount} rules and {PolicyCount} policies",
                route, fareClass, applicableRules.Count, applicablePolicies.Count);

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pricing configuration for route {Route} fare class {FareClass}", route, fareClass);
            return new PricingConfiguration
            {
                Rules = new List<PricingRule>(),
                Policies = new List<PricingPolicy>(),
                LastUpdated = DateTime.UtcNow,
                Version = "error",
                Settings = new Dictionary<string, object>()
            };
        }
    }

    public async Task UpdatePricingRuleAsync(PricingRule rule, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingRule = await _context.Set<PricingRule>()
                .FirstOrDefaultAsync(r => r.Id == rule.Id, cancellationToken);

            if (existingRule != null)
            {
                // Update existing rule
                _context.Entry(existingRule).CurrentValues.SetValues(rule);
                existingRule.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new rule
                rule.CreatedAt = DateTime.UtcNow;
                rule.UpdatedAt = DateTime.UtcNow;
                _context.Set<PricingRule>().Add(rule);
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidate cache
            InvalidatePricingCache();
            
            _logger.LogInformation("Updated pricing rule {RuleId}: {RuleName}", rule.RuleId, rule.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pricing rule {RuleId}", rule.RuleId);
            throw;
        }
    }

    public async Task UpdatePricingPolicyAsync(PricingPolicy policy, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingPolicy = await _context.Set<PricingPolicy>()
                .FirstOrDefaultAsync(p => p.Id == policy.Id, cancellationToken);

            if (existingPolicy != null)
            {
                // Update existing policy
                _context.Entry(existingPolicy).CurrentValues.SetValues(policy);
                existingPolicy.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new policy
                policy.CreatedAt = DateTime.UtcNow;
                policy.UpdatedAt = DateTime.UtcNow;
                _context.Set<PricingPolicy>().Add(policy);
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidate cache
            InvalidatePricingCache();
            
            _logger.LogInformation("Updated pricing policy {PolicyId}: {PolicyName}", policy.PolicyId, policy.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pricing policy {PolicyId}", policy.PolicyId);
            throw;
        }
    }

    private async Task<Dictionary<string, object>> GetPricingSettingsAsync(string route, string fareClass, CancellationToken cancellationToken)
    {
        // This could be extended to load route-specific and fare class-specific settings from database
        var settings = new Dictionary<string, object>
        {
            ["route"] = route,
            ["fare_class"] = fareClass,
            ["currency"] = "USD",
            ["tax_calculation_enabled"] = true,
            ["fee_calculation_enabled"] = true,
            ["promotional_discounts_enabled"] = true,
            ["demand_based_pricing_enabled"] = true,
            ["seasonal_adjustments_enabled"] = true,
            ["weekend_surcharges_enabled"] = true,
            ["last_updated"] = DateTime.UtcNow
        };

        // Add route-specific settings
        if (IsInternationalRoute(route))
        {
            settings["is_international"] = true;
            settings["international_taxes_enabled"] = true;
            settings["visa_requirements_check"] = true;
        }
        else
        {
            settings["is_international"] = false;
            settings["domestic_taxes_enabled"] = true;
        }

        // Add fare class-specific settings
        settings.Add("fare_class_settings", GetFareClassSettings(fareClass));

        return settings;
    }

    private bool IsInternationalRoute(string route)
    {
        // Simplified logic - in real implementation, this would check against airport/country data
        var internationalRoutes = new[] { "JFK-LHR", "LAX-NRT", "SFO-CDG", "NYC-LON", "LAX-NRT" };
        return internationalRoutes.Contains(route);
    }

    private Dictionary<string, object> GetFareClassSettings(string fareClass)
    {
        return fareClass.ToUpper() switch
        {
            "ECONOMY" => new Dictionary<string, object>
            {
                ["change_fee_enabled"] = true,
                ["refund_fee_enabled"] = true,
                ["advance_purchase_required"] = true,
                ["minimum_advance_days"] = 7,
                ["promotional_eligibility"] = true
            },
            "BUSINESS" => new Dictionary<string, object>
            {
                ["change_fee_enabled"] = false,
                ["refund_fee_enabled"] = false,
                ["advance_purchase_required"] = false,
                ["minimum_advance_days"] = 0,
                ["promotional_eligibility"] = false,
                ["priority_services_included"] = true
            },
            "FIRST" => new Dictionary<string, object>
            {
                ["change_fee_enabled"] = false,
                ["refund_fee_enabled"] = false,
                ["advance_purchase_required"] = false,
                ["minimum_advance_days"] = 0,
                ["promotional_eligibility"] = false,
                ["priority_services_included"] = true,
                ["luxury_services_included"] = true
            },
            _ => new Dictionary<string, object>
            {
                ["change_fee_enabled"] = true,
                ["refund_fee_enabled"] = true,
                ["advance_purchase_required"] = true,
                ["minimum_advance_days"] = 7,
                ["promotional_eligibility"] = true
            }
        };
    }

    private string GenerateConfigurationVersion(List<PricingRule> rules, List<PricingPolicy> policies)
    {
        var ruleHash = string.Join(",", rules.Select(r => $"{r.RuleId}:{r.UpdatedAt:yyyyMMddHHmmss}"));
        var policyHash = string.Join(",", policies.Select(p => $"{p.PolicyId}:{p.UpdatedAt:yyyyMMddHHmmss}"));
        var combinedHash = $"{ruleHash}|{policyHash}";
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedHash));
        return Convert.ToBase64String(hashBytes)[..8]; // First 8 characters
    }

    private void InvalidatePricingCache()
    {
        // Remove all pricing-related cache entries
        var cacheKeys = new[]
        {
            "pricing_rules_active",
            "pricing_policies_active"
        };

        foreach (var key in cacheKeys)
        {
            _cache.Remove(key);
        }

        // Also remove route-specific configurations
        // In a real implementation, you might want to track cache keys more systematically
        _logger.LogDebug("Invalidated pricing configuration cache");
    }
}

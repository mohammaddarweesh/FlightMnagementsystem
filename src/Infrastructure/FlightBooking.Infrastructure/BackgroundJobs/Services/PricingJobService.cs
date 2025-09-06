using FlightBooking.Application.Pricing.Services;
using FlightBooking.Application.Pricing.Queries;
using FlightBooking.Infrastructure.BackgroundJobs.Attributes;
using FlightBooking.Infrastructure.BackgroundJobs.Configuration;
using FlightBooking.Infrastructure.Data;
using FlightBooking.Domain.Promotions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.BackgroundJobs.Services;

public class PricingJobService
{
    private readonly IPricingService _pricingService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PricingJobService> _logger;

    public PricingJobService(IPricingService pricingService, ApplicationDbContext context, ILogger<PricingJobService> logger)
    {
        _pricingService = pricingService;
        _context = context;
        _logger = logger;
    }

    [PricingRetry]
    [Queue(HangfireQueues.Pricing)]
    public async Task UpdateFlightPricingAsync(Guid flightId, string? correlationId = null)
    {
        _logger.LogInformation("Starting flight pricing update for {FlightId}", flightId);
        
        var query = new CalculatePricingQuery { FlightId = flightId };
        await _pricingService.CalculatePricingAsync(query);
        
        _logger.LogInformation("Flight pricing update completed for {FlightId}", flightId);
    }

    [PricingRetry]
    [Queue(HangfireQueues.Pricing)]
    public async Task ExpireOldPromotionsAsync(string? correlationId = null)
    {
        _logger.LogInformation("Starting promotion expiration check");
        
        var expiredPromotions = await _context.Promotions
            .Where(p => p.IsActive && p.ExpiryDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var promotion in expiredPromotions)
        {
            promotion.Deactivate("System");
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Expired {Count} promotions", expiredPromotions.Count);
    }

    [PricingRetry]
    [Queue(HangfireQueues.Pricing)]
    public async Task WarmPricingCacheAsync(string? correlationId = null)
    {
        _logger.LogInformation("Starting pricing cache warming");

        // Cache warming logic would go here
        // This would typically pre-calculate pricing for popular routes

        _logger.LogInformation("Pricing cache warming completed");
    }

    [PricingRetry]
    [Queue(HangfireQueues.Pricing)]
    public async Task UpdateSeasonalPricingAsync(string? correlationId = null)
    {
        _logger.LogInformation("Starting seasonal pricing update");

        // Seasonal pricing update logic would go here
        // This would typically adjust pricing based on seasonal demand

        _logger.LogInformation("Seasonal pricing update completed");
    }
}

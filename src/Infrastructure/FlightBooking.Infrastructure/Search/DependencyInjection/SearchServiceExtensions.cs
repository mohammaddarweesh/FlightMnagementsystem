using FlightBooking.Application.Search.Events;
using FlightBooking.Application.Search.Services;
using FlightBooking.Infrastructure.Search.EventHandlers;
using FlightBooking.Infrastructure.Search.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FlightBooking.Infrastructure.Search.DependencyInjection;

public static class SearchServiceExtensions
{
    public static IServiceCollection AddSearchServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Redis Configuration
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configuration = ConfigurationOptions.Parse(redisConnectionString);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectRetry = 3;
            configuration.ConnectTimeout = 5000;
            configuration.SyncTimeout = 5000;
            configuration.AsyncTimeout = 5000;
            configuration.KeepAlive = 60;
            
            return ConnectionMultiplexer.Connect(configuration);
        });

        // Cache Services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IFlightSearchCacheService, FlightSearchCacheService>();

        // Search Services
        services.AddScoped<IFlightSearchService, FlightSearchService>();

        // Event Handlers for Cache Invalidation
        services.AddScoped<INotificationHandler<FlightUpdatedEvent>, FlightUpdatedEventHandler>();
        services.AddScoped<INotificationHandler<FareClassUpdatedEvent>, FareClassUpdatedEventHandler>();
        services.AddScoped<INotificationHandler<BookingConfirmedEvent>, BookingConfirmedEventHandler>();
        services.AddScoped<INotificationHandler<BookingCancelledEvent>, BookingCancelledEventHandler>();
        services.AddScoped<INotificationHandler<SeatStatusChangedEvent>, SeatStatusChangedEventHandler>();
        services.AddScoped<INotificationHandler<BulkPriceUpdateEvent>, BulkPriceUpdateEventHandler>();

        return services;
    }

    public static IServiceCollection AddSearchCacheConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure cache TTL settings
        services.Configure<SearchCacheOptions>(options =>
        {
            options.SearchResultsTtl = TimeSpan.FromMinutes(configuration.GetValue<int>("Cache:SearchResultsTtlMinutes", 3));
            options.AvailabilityTtl = TimeSpan.FromMinutes(configuration.GetValue<int>("Cache:AvailabilityTtlMinutes", 2));
            options.PopularRoutesTtl = TimeSpan.FromMinutes(configuration.GetValue<int>("Cache:PopularRoutesTtlMinutes", 10));
            options.SuggestionsTtl = TimeSpan.FromMinutes(configuration.GetValue<int>("Cache:SuggestionsTtlMinutes", 30));
            options.PricingTtl = TimeSpan.FromMinutes(configuration.GetValue<int>("Cache:PricingTtlMinutes", 1));
            options.EnableCaching = configuration.GetValue<bool>("Cache:EnableCaching", true);
            options.EnableETagSupport = configuration.GetValue<bool>("Cache:EnableETagSupport", true);
            options.MaxCacheKeyLength = configuration.GetValue<int>("Cache:MaxCacheKeyLength", 250);
        });

        return services;
    }

    public static IServiceCollection AddSearchHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis_cache")
            .AddCheck<SearchServiceHealthCheck>("search_service");

        return services;
    }
}

public class SearchCacheOptions
{
    public TimeSpan SearchResultsTtl { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan AvailabilityTtl { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan PopularRoutesTtl { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan SuggestionsTtl { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan PricingTtl { get; set; } = TimeSpan.FromMinutes(1);
    public bool EnableCaching { get; set; } = true;
    public bool EnableETagSupport { get; set; } = true;
    public int MaxCacheKeyLength { get; set; } = 250;
}

public class RedisHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            await database.PingAsync();

            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var info = await server.InfoAsync();
            var memoryUsage = "unknown";

            foreach (var section in info)
            {
                foreach (var item in section)
                {
                    if (item.Key == "used_memory")
                    {
                        memoryUsage = item.Value;
                        break;
                    }
                }
            }

            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                $"Redis is healthy. Memory usage: {memoryUsage}");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Redis is unhealthy", ex);
        }
    }
}

public class SearchServiceHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IFlightSearchCacheService _cacheService;

    public SearchServiceHealthCheck(IFlightSearchCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _cacheService.GetSearchCacheStatisticsAsync(cancellationToken);
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                $"Search service is healthy. Cache hit ratio: {stats.SearchHitRatio:P2}");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Search service is unhealthy", ex);
        }
    }
}

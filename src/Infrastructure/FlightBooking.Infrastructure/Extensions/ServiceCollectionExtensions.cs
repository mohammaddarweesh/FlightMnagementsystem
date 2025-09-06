using FlightBooking.Application.Identity.Interfaces;
using FlightBooking.Application.Bookings.Services;
using FlightBooking.Infrastructure.Analytics.Extensions;
using FlightBooking.Infrastructure.Data;
using FlightBooking.Infrastructure.Identity;
using FlightBooking.Infrastructure.Bookings.Services;
using FlightBooking.Infrastructure.Bookings.Repositories;
using FlightBooking.Infrastructure.Redis.Configuration;
using FlightBooking.Infrastructure.Redis.Services;
using FlightBooking.Infrastructure.BackgroundJobs.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
// using RedLockNet.SERedis;
// using RedLockNet.SERedis.Configuration;
using System.Reflection;
using StackExchange.Redis;

namespace FlightBooking.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services including Entity Framework, Redis, and background jobs.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Entity Framework
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Redis Configuration
        services.Configure<RedisConfiguration>(configuration.GetSection(RedisConfiguration.SectionName));

        // Redis Connection
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisConfig = provider.GetRequiredService<IOptions<RedisConfiguration>>().Value;
            var configurationOptions = ConfigurationOptions.Parse(redisConfig.ConnectionString);
            configurationOptions.ConnectTimeout = (int)redisConfig.ConnectTimeout.TotalMilliseconds;
            configurationOptions.CommandMap = CommandMap.Create(new HashSet<string> { "INFO", "CONFIG" }, available: false);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectRetry = redisConfig.RetryCount;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        // Redis Services
        services.AddRedisServices();

        // Hangfire Services
        services.AddHangfireServices(configuration);

        // Identity Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserService, UserService>();

        // Booking Services
        services.AddScoped<IBookingService, SimplifiedBookingService>();
        services.AddScoped<ISeatInventoryService, SeatInventoryService>();
        services.AddScoped<ISeatInventoryRepository, SeatInventoryRepository>();

        // Memory Cache
        services.AddMemoryCache();

        // TODO: Redis Distributed Locks - Configure RedLock.net when needed
        // services.AddSingleton<IRedLockFactory>(provider => ...);

        // Database Seeding
        services.AddScoped<DatabaseSeeder>();

        // Register MediatR handlers from Infrastructure assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // TODO: Add Hangfire when package is available
        // services.AddHangfire(config =>
        //     config.UsePostgreSqlStorage(configuration.GetConnectionString("Hangfire")));
        // services.AddHangfireServer();

        // Add Analytics services
        services.AddAnalyticsServices(configuration);
        services.AddAnalyticsHealthChecks(configuration);

        return services;
    }

    /// <summary>
    /// Add Redis services to the service collection
    /// </summary>
    public static IServiceCollection AddRedisServices(this IServiceCollection services)
    {
        // Core Redis services
        services.AddSingleton<IRedisService, RedisService>();
        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        services.AddSingleton<IRedisSessionService, RedisSessionService>();
        services.AddSingleton<IRedisMetricsService, RedisMetricsService>();

        // Distributed lock services (commented out until RedLock.net compatibility is resolved)
        // services.AddSingleton<IDistributedLockService, DistributedLockService>();
        // services.AddSingleton<ISeatLockService, SeatLockService>();
        // services.AddSingleton<IPromotionLockService, PromotionLockService>();

        return services;
    }
}

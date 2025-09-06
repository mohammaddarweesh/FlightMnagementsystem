using FlightBooking.Api.Extensions;
using FlightBooking.Api.Middleware;
using FlightBooking.Application.Extensions;
using FlightBooking.BackgroundJobs.Audit;
using FlightBooking.Infrastructure.Data;
using FlightBooking.Infrastructure.Extensions;
using FlightBooking.Infrastructure.Redis.RateLimit;
using FlightBooking.Infrastructure.BackgroundJobs.Extensions;
using FlightBooking.Infrastructure.Logging;
using FlightBooking.Infrastructure.Search.DependencyInjection;
using FlightBooking.Infrastructure.Pricing.DependencyInjection;
using Hellang.Middleware.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// Add distributed cache for rate limiting
builder.Services.AddDistributedMemoryCache();

// Add custom services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

// Add search and caching services
builder.Services.AddSearchServices(builder.Configuration);
builder.Services.AddSearchCacheConfiguration(builder.Configuration);
builder.Services.AddSearchHealthChecks();

// Add pricing services
builder.Services.AddPricingServices(builder.Configuration);
builder.Services.AddPricingHealthChecks();

// Add audit services
builder.Services.AddHttpContextAccessor();
// TODO: Add Hangfire configuration for background processing
// builder.Services.AddHostedService<AuditJobScheduler>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseProblemDetails();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseSerilogRequestLogging();

// Custom middleware
app.UseMiddleware<GuestIdMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<RedisRateLimitMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Configure Hangfire Dashboard
app.UseHangfireDashboard(builder.Configuration);

// Skip recurring jobs in test mode
if (!builder.Configuration.GetValue<bool>("Testing:SkipRecurringJobs"))
{
    app.ConfigureRecurringJobs(builder.Configuration);
}

// Run migrations and seed database (skip in test mode)
if (!builder.Configuration.GetValue<bool>("Testing:SkipRecurringJobs"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation("Running database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed successfully");

        // Seed database
        logger.LogInformation("Starting database seeding...");
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database initialization");
        throw;
    }
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }

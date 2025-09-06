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
using Npgsql;
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

// Create databases and run migrations BEFORE Hangfire initialization
if (!builder.Configuration.GetValue<bool>("Testing:SkipRecurringJobs"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Step 1: Create Hangfire database FIRST (before main database migration)
        logger.LogInformation("Creating Hangfire database...");
        await CreateHangfireDatabaseAsync(builder.Configuration, logger);
        logger.LogInformation("Hangfire database creation completed");

        // Step 2: Run main database migrations
        logger.LogInformation("Running database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed successfully");

        // Step 3: Seed database
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

// Configure Hangfire Dashboard (after database is ready)
app.UseHangfireDashboard(builder.Configuration);

// Skip recurring jobs in test mode
if (!builder.Configuration.GetValue<bool>("Testing:SkipRecurringJobs"))
{
    app.ConfigureRecurringJobs(builder.Configuration);
}

app.Run();

// Helper method to create Hangfire database
static async Task CreateHangfireDatabaseAsync(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger logger)
{
    try
    {
        string postgresConnectionString = "Host=localhost;Database=postgres;Username=postgres;Password=6482297";
        using NpgsqlConnection connection = new NpgsqlConnection(postgresConnectionString);
        await connection.OpenAsync();

        // Check if database exists
        using NpgsqlCommand checkCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'flightbookinghangfire_mohammaddarweesh'", connection);
        object? exists = await checkCmd.ExecuteScalarAsync();

        if (exists == null)
        {
            // Create the database
            using NpgsqlCommand createCmd = new NpgsqlCommand("CREATE DATABASE flightbookinghangfire_mohammaddarweesh", connection);
            await createCmd.ExecuteNonQueryAsync();
            logger.LogInformation("Hangfire database 'flightbookinghangfire_mohammaddarweesh' created successfully.");
        }
        else
        {
            logger.LogInformation("Hangfire database 'flightbookinghangfire_mohammaddarweesh' already exists.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating Hangfire database: {Message}", ex.Message);
        throw;
    }
}

// Make Program class accessible for integration tests
public partial class Program { }

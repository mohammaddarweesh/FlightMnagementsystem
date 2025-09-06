using FlightBooking.Infrastructure.BackgroundJobs.Extensions;
using FlightBooking.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlightBooking.Workers;

/// <summary>
/// Dedicated Hangfire worker host for background job processing
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog early
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/worker-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting Flight Booking Worker Host");

            var host = CreateHostBuilder(args).Build();

            // Ensure database is created and migrated
            await EnsureDatabaseAsync(host);

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Worker host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                    optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // Add infrastructure services
                services.AddInfrastructureServices(configuration);

                // Add Hangfire worker services
                services.AddHangfireWorkerHost(configuration);

                // Add health checks
                services.AddHealthChecks()
                    .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!)
                    .AddNpgSql(configuration.GetConnectionString("Hangfire")!, name: "hangfire-db")
                    .AddHangfire(options =>
                    {
                        options.MinimumAvailableServers = 1;
                    });

                // Add application insights or other monitoring
                if (!string.IsNullOrEmpty(configuration["ApplicationInsights:ConnectionString"]))
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                }
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
                
                // Add additional logging providers if needed
                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.AddConsole();
                    logging.AddDebug();
                }
            });

    private static async Task EnsureDatabaseAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Running database migrations...");

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure database is ready");
            throw;
        }
    }
}

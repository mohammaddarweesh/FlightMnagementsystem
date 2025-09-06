using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlightBooking.Infrastructure.Data;
using Hangfire;
using Hangfire.MemoryStorage;

namespace FlightBooking.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// </summary>
/// <typeparam name="TProgram">The program type</typeparam>
public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:Hangfire"] = "DataSource=:memory:",
                ["Hangfire:ConnectionString"] = "DataSource=:memory:",
                ["Hangfire:Dashboard:Enabled"] = "false",
                ["Testing:SkipRecurringJobs"] = "true",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
                options.EnableSensitiveDataLogging();
            });

            // Remove existing Hangfire services
            var hangfireDescriptors = services.Where(d => 
                d.ServiceType.FullName?.Contains("Hangfire") == true).ToList();
            foreach (var hangfireDescriptor in hangfireDescriptors)
            {
                services.Remove(hangfireDescriptor);
            }

            // Add Hangfire with in-memory storage for testing
            services.AddHangfire(config =>
            {
                config.UseMemoryStorage();
                config.UseFilter(new AutomaticRetryAttribute { Attempts = 0 });
            });

            // Add Hangfire server for testing
            services.AddHangfireServer(options =>
            {
                options.Queues = new[] { "default" };
                options.WorkerCount = 1;
            });

            // Add distributed cache for testing (required by RateLimitingMiddleware)
            services.AddDistributedMemoryCache();

            // Override authentication for testing - allow anonymous access
            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", options => { });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true) // Allow all requests
                    .Build();
            });

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<TestWebApplicationFactory<TProgram>>>();

            // Ensure the database is created
            try
            {
                db.Database.EnsureCreated();
                SeedTestData(db);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the database with test data");
                throw;
            }
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Seed test data into the database
    /// </summary>
    /// <param name="context">The database context</param>
    private static void SeedTestData(ApplicationDbContext context)
    {
        // Add test data if needed
        if (!context.Users.Any())
        {
            // Add a test user
            var testUser = new FlightBooking.Domain.Identity.User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                PasswordHash = "test-hash",
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(testUser);
        }

        if (!context.Airports.Any())
        {
            // Add test airports
            var airports = new[]
            {
                new FlightBooking.Domain.Flights.Airport
                {
                    Id = Guid.NewGuid(),
                    IataCode = "JFK",
                    Name = "John F. Kennedy International Airport",
                    City = "New York",
                    Country = "USA",
                    TimeZone = "America/New_York",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new FlightBooking.Domain.Flights.Airport
                {
                    Id = Guid.NewGuid(),
                    IataCode = "LAX",
                    Name = "Los Angeles International Airport",
                    City = "Los Angeles",
                    Country = "USA",
                    TimeZone = "America/Los_Angeles",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Airports.AddRange(airports);
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Get a scoped service from the test container
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance</returns>
    public T GetScopedService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get the test database context
    /// </summary>
    /// <returns>The database context</returns>
    public ApplicationDbContext GetDbContext()
    {
        return GetScopedService<ApplicationDbContext>();
    }

    /// <summary>
    /// Reset the database to a clean state
    /// </summary>
    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        SeedTestData(context);
    }
}

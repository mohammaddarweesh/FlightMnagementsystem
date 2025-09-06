using FlightBooking.Application.Identity.Interfaces;
using FlightBooking.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger,
        ILoggerFactory loggerFactory)
    {
        _context = context;
        _passwordService = passwordService;
        _configuration = configuration;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Seed roles (if not already seeded by migration)
            await SeedRolesAsync();

            // Seed admin user
            await SeedAdminUserAsync();

            // Seed flight data
            var flightSeederLogger = _loggerFactory.CreateLogger<FlightSeeder>();
            var flightSeeder = new FlightSeeder(_context, flightSeederLogger);
            await flightSeeder.SeedAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new { Name = Role.Names.Admin, Description = "System administrator with full access" },
            new { Name = Role.Names.Staff, Description = "Staff member with limited administrative access" },
            new { Name = Role.Names.Customer, Description = "Regular customer with booking access" }
        };

        foreach (var roleData in roles)
        {
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleData.Name);

            if (existingRole == null)
            {
                var role = new Role
                {
                    Name = roleData.Name,
                    Description = roleData.Description,
                    IsActive = true
                };

                _context.Roles.Add(role);
                _logger.LogInformation("Added role: {RoleName}", roleData.Name);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = _configuration["AdminUser:Email"] ?? "admin@flightbooking.com";
        var adminPassword = _configuration["AdminUser:Password"] ?? "Admin123!@#";
        var adminFirstName = _configuration["AdminUser:FirstName"] ?? "System";
        var adminLastName = _configuration["AdminUser:LastName"] ?? "Administrator";

        // Check if admin user already exists
        var existingAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin user already exists: {Email}", adminEmail);
            return;
        }

        // Create admin user
        var adminUser = new User
        {
            Email = adminEmail,
            FirstName = adminFirstName,
            LastName = adminLastName,
            PasswordHash = _passwordService.HashPassword(adminPassword),
            EmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(); // Save to get the user ID

        // Assign admin role
        var adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == Role.Names.Admin);

        if (adminRole != null)
        {
            var userRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            _logger.LogInformation("Created admin user: {Email} with password: {Password}", adminEmail, adminPassword);
        }
        else
        {
            _logger.LogError("Admin role not found. Cannot assign admin role to user.");
        }
    }
}

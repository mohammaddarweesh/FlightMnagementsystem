using System.Text;
using FluentValidation;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FlightBooking.Api.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register API services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds API services including authentication, problem details, and health checks.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Problem Details
        services.AddProblemDetails(options =>
        {
            options.IncludeExceptionDetails = (context, exception) =>
                context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
        });

        // JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = configuration.GetSection("JwtSettings");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "default-key")),
                };
            });

        // Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            options.AddPolicy("StaffPolicy", policy => policy.RequireRole("Admin", "Staff"));
            options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("Admin", "Staff", "Customer"));
            options.AddPolicy("EmailVerifiedPolicy", policy =>
                policy.RequireClaim("email_verified", "true"));
        });

        // Rate Limiting - Simple implementation for now
        // TODO: Implement proper rate limiting when Microsoft.AspNetCore.RateLimiting is available

        // Health Checks
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!)
            .AddRedis(configuration.GetConnectionString("Redis")!);

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        // Add Authorization Policies for Analytics
        services.AddAuthorization(options =>
        {
            // Admin policy - full access to all analytics
            options.AddPolicy("Admin", policy =>
                policy.RequireRole("Admin"));

            // Staff policy - read access to analytics
            options.AddPolicy("Staff", policy =>
                policy.RequireRole("Staff", "Manager", "Analyst"));

            // AdminOrStaff policy - combined access for analytics endpoints
            options.AddPolicy("AdminOrStaff", policy =>
                policy.RequireRole("Admin", "Staff", "Manager", "Analyst"));

            // Analytics policy - specific for analytics access
            options.AddPolicy("Analytics", policy =>
                policy.RequireRole("Admin", "Staff", "Manager", "Analyst")
                      .RequireClaim("permission", "analytics:read"));

            // Analytics Export policy - for CSV exports
            options.AddPolicy("AnalyticsExport", policy =>
                policy.RequireRole("Admin", "Staff", "Manager")
                      .RequireClaim("permission", "analytics:export"));

            // Analytics Admin policy - for sensitive operations
            options.AddPolicy("AnalyticsAdmin", policy =>
                policy.RequireRole("Admin")
                      .RequireClaim("permission", "analytics:admin"));
        });

        return services;
    }
}

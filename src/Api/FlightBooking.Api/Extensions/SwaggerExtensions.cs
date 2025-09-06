using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace FlightBooking.Api.Extensions;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI documentation
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Add comprehensive Swagger documentation configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // API Information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Flight Booking Management System API",
                Description = "A comprehensive flight booking and management system with analytics, pricing, and booking capabilities.",
                
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Group APIs by area
            options.SwaggerDoc("analytics", new OpenApiInfo
            {
                Version = "v1",
                Title = "Analytics API",
                Description = "Analytics and reporting endpoints for revenue, bookings, demographics, and performance metrics."
            });

            options.SwaggerDoc("bookings", new OpenApiInfo
            {
                Version = "v1",
                Title = "Bookings API", 
                Description = "Flight booking management endpoints for creating, updating, and managing reservations."
            });

            options.SwaggerDoc("flights", new OpenApiInfo
            {
                Version = "v1",
                Title = "Flights API",
                Description = "Flight search, scheduling, and management endpoints."
            });

            options.SwaggerDoc("identity", new OpenApiInfo
            {
                Version = "v1",
                Title = "Identity API",
                Description = "User authentication, authorization, and profile management endpoints."
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Group endpoints by controller tags
            options.TagActionsBy(api =>
            {
                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                return controllerName switch
                {
                    "Analytics" or "AnalyticsExport" => new[] { "Analytics" },
                    "Bookings" => new[] { "Bookings" },
                    "Flights" => new[] { "Flights" },
                    "Auth" or "Users" => new[] { "Identity" },
                    _ => new[] { controllerName ?? "General" }
                };
            });

            // Document which API group each endpoint belongs to
            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
                return docName switch
                {
                    "analytics" => controllerName is "Analytics" or "AnalyticsExport",
                    "bookings" => controllerName is "Bookings",
                    "flights" => controllerName is "Flights",
                    "identity" => controllerName is "Auth" or "Users",
                    "v1" => true, // Include all in main API doc
                    _ => false
                };
            });

            // Add security definitions for JWT Bearer tokens
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Add examples and better error documentation
            options.EnableAnnotations();
            options.UseInlineDefinitionsForEnums();

            // Custom schema IDs to avoid conflicts
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

            // Add operation filters for better documentation
            options.OperationFilter<SwaggerDefaultValues>();
            options.DocumentFilter<SwaggerGroupingDocumentFilter>();
        });

        return services;
    }

    /// <summary>
    /// Configure Swagger UI with multiple API groups
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "api-docs/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            // Main API documentation
            options.SwaggerEndpoint("/api-docs/v1/swagger.json", "Flight Booking API v1");
            
            // Grouped API documentation
            options.SwaggerEndpoint("/api-docs/analytics/swagger.json", "Analytics API");
            options.SwaggerEndpoint("/api-docs/bookings/swagger.json", "Bookings API");
            options.SwaggerEndpoint("/api-docs/flights/swagger.json", "Flights API");
            options.SwaggerEndpoint("/api-docs/identity/swagger.json", "Identity API");

            options.RoutePrefix = "api-docs";
            options.DocumentTitle = "Flight Booking Management System API";
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelExpandDepth(2);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableValidator();
            
            // Custom CSS for better appearance
            options.InjectStylesheet("/swagger-ui/custom.css");
        });

        return app;
    }
}

/// <summary>
/// Operation filter to add default values and examples to Swagger documentation
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated = false; // Set to false by default, can be enhanced later

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (responseType.Type != null && responseType.Type != typeof(void))
                {
                    var schema = context.SchemaGenerator.GenerateSchema(responseType.Type, context.SchemaRepository);
                    response.Content[contentType].Schema = schema;
                }
            }
        }
    }
}

/// <summary>
/// Document filter to organize API groups
/// </summary>
public class SwaggerGroupingDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add custom tags for better organization
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new() { Name = "Analytics", Description = "Analytics and reporting operations" },
            new() { Name = "Bookings", Description = "Flight booking management operations" },
            new() { Name = "Flights", Description = "Flight search and management operations" },
            new() { Name = "Identity", Description = "Authentication and user management operations" }
        };
    }
}

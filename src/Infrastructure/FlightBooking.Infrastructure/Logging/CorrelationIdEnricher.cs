using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace FlightBooking.Infrastructure.Logging;

public class CorrelationIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        // Add correlation ID
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId.ToString()));
        }

        // Add user context (minimal PII)
        var userId = httpContext.User?.FindFirst("sub")?.Value ?? 
                    httpContext.User?.FindFirst("nameid")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
        }

        // Add guest ID if no user
        var guestId = httpContext.Items["GuestId"] as string;
        if (!string.IsNullOrEmpty(guestId) && string.IsNullOrEmpty(userId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("GuestId", guestId));
        }

        // Add request path
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", httpContext.Request.Path.Value));

        // Add HTTP method
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("HttpMethod", httpContext.Request.Method));

        // Add IP address (hashed for privacy)
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var hashedIp = HashIpAddress(ipAddress);
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientIpHash", hashedIp));
        }

        // Add user agent (truncated)
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        if (!string.IsNullOrEmpty(userAgent))
        {
            var truncatedUserAgent = userAgent.Length > 100 ? userAgent[..100] + "..." : userAgent;
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserAgent", truncatedUserAgent));
        }
    }

    private static string HashIpAddress(string ipAddress)
    {
        // Simple hash for IP privacy - not cryptographically secure but sufficient for logging
        return ipAddress.GetHashCode().ToString("X8");
    }
}

using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Text.Json;

namespace FlightBooking.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly Dictionary<string, RateLimitRule> _rules;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _rules = new Dictionary<string, RateLimitRule>
        {
            { "/api/auth/login", new RateLimitRule { MaxRequests = 5, WindowMinutes = 15 } },
            { "/api/auth/register", new RateLimitRule { MaxRequests = 3, WindowMinutes = 60 } },
            { "/api/auth/forgot-password", new RateLimitRule { MaxRequests = 3, WindowMinutes = 60 } },
            { "/api/auth/reset-password", new RateLimitRule { MaxRequests = 5, WindowMinutes = 60 } },
            { "/api/auth/resend-verification", new RateLimitRule { MaxRequests = 3, WindowMinutes = 60 } }
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        if (path != null && _rules.TryGetValue(path, out var rule))
        {
            var clientId = GetClientIdentifier(context);
            var key = $"rate_limit:{path}:{clientId}";
            
            var currentCount = await GetCurrentRequestCount(key);
            
            if (currentCount >= rule.MaxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for {Path} by {ClientId}", path, clientId);
                await HandleRateLimitExceeded(context, rule);
                return;
            }
            
            await IncrementRequestCount(key, rule.WindowMinutes);
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get user ID from claims first
        var userId = context.User?.FindFirst("sub")?.Value ?? 
                    context.User?.FindFirst("nameid")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    private async Task<int> GetCurrentRequestCount(string key)
    {
        try
        {
            var countString = await _cache.GetStringAsync(key);
            return int.TryParse(countString, out var count) ? count : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate limit count for key: {Key}", key);
            return 0;
        }
    }

    private async Task IncrementRequestCount(string key, int windowMinutes)
    {
        try
        {
            var currentCount = await GetCurrentRequestCount(key);
            var newCount = currentCount + 1;
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMinutes)
            };
            
            await _cache.SetStringAsync(key, newCount.ToString(), options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing rate limit count for key: {Key}", key);
        }
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitRule rule)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Maximum {rule.MaxRequests} requests allowed per {rule.WindowMinutes} minutes.",
            retryAfter = TimeSpan.FromMinutes(rule.WindowMinutes).TotalSeconds
        };
        
        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }

    private class RateLimitRule
    {
        public int MaxRequests { get; set; }
        public int WindowMinutes { get; set; }
    }
}

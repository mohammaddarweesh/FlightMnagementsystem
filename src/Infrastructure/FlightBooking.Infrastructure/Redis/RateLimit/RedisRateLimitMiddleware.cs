using FlightBooking.Infrastructure.Redis.Configuration;
using FlightBooking.Infrastructure.Redis.Keys;
using FlightBooking.Infrastructure.Redis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace FlightBooking.Infrastructure.Redis.RateLimit;

/// <summary>
/// Redis-backed rate limiting middleware using sliding window algorithm
/// </summary>
public class RedisRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRedisService _redisService;
    private readonly ILogger<RedisRateLimitMiddleware> _logger;
    private readonly RedisConfiguration _config;

    public RedisRateLimitMiddleware(
        RequestDelegate next,
        IRedisService redisService,
        IOptions<RedisConfiguration> config,
        ILogger<RedisRateLimitMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = GetEndpointIdentifier(context);
        var clientId = GetClientIdentifier(context);

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(clientId))
        {
            await _next(context);
            return;
        }

        var policy = GetRateLimitPolicy(endpoint);
        if (policy == null)
        {
            await _next(context);
            return;
        }

        try
        {
            var rateLimitResult = await CheckRateLimitAsync(endpoint, clientId, policy);

            // Add rate limit headers
            AddRateLimitHeaders(context, rateLimitResult);

            if (!rateLimitResult.IsAllowed)
            {
                await HandleRateLimitExceeded(context, rateLimitResult);
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware for endpoint {Endpoint}, client {ClientId}", endpoint, clientId);
            // Continue processing on error to avoid blocking requests
            await _next(context);
        }
    }

    private async Task<RateLimitResult> CheckRateLimitAsync(string endpoint, string clientId, RateLimitPolicy policy)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = GetWindowStart(now, policy.Window, policy.SegmentsPerWindow);
        
        // Use sliding window algorithm with multiple segments
        var segments = GetWindowSegments(windowStart, policy.Window, policy.SegmentsPerWindow);
        var totalRequests = 0L;

        foreach (var segment in segments)
        {
            var segmentKey = RedisKeyBuilder.RateLimitKey(_config.KeyPrefix, endpoint, clientId, segment);
            var segmentCount = await GetSegmentCountAsync(segmentKey);
            totalRequests += segmentCount;
        }

        var isAllowed = totalRequests < policy.PermitLimit;
        
        if (isAllowed)
        {
            // Increment current segment
            var currentSegmentKey = RedisKeyBuilder.RateLimitKey(_config.KeyPrefix, endpoint, clientId, windowStart);
            await IncrementSegmentAsync(currentSegmentKey, policy.Window);
        }

        var resetTime = windowStart + policy.Window.Ticks;
        var remaining = Math.Max(0, policy.PermitLimit - totalRequests - (isAllowed ? 1 : 0));

        return new RateLimitResult
        {
            IsAllowed = isAllowed,
            Limit = policy.PermitLimit,
            Remaining = (int)remaining,
            ResetTime = DateTimeOffset.FromUnixTimeMilliseconds(resetTime / TimeSpan.TicksPerMillisecond),
            RetryAfter = isAllowed ? null : CalculateRetryAfter(segments, policy)
        };
    }

    private async Task<long> GetSegmentCountAsync(string segmentKey)
    {
        try
        {
            var countStr = await _redisService.GetStringAsync(segmentKey);
            return long.TryParse(countStr, out var count) ? count : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get segment count for key {Key}", segmentKey);
            return 0;
        }
    }

    private async Task IncrementSegmentAsync(string segmentKey, TimeSpan window)
    {
        try
        {
            await _redisService.IncrementAsync(segmentKey);
            await _redisService.ExpireAsync(segmentKey, window);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to increment segment for key {Key}", segmentKey);
        }
    }

    private static long GetWindowStart(DateTimeOffset now, TimeSpan window, int segmentsPerWindow)
    {
        var segmentDuration = window.Ticks / segmentsPerWindow;
        return (now.Ticks / segmentDuration) * segmentDuration;
    }

    private static long[] GetWindowSegments(long windowStart, TimeSpan window, int segmentsPerWindow)
    {
        var segmentDuration = window.Ticks / segmentsPerWindow;
        var segments = new long[segmentsPerWindow];
        
        for (int i = 0; i < segmentsPerWindow; i++)
        {
            segments[i] = windowStart - (i * segmentDuration);
        }
        
        return segments;
    }

    private static TimeSpan? CalculateRetryAfter(long[] segments, RateLimitPolicy policy)
    {
        if (segments.Length == 0) return null;
        
        var oldestSegment = segments[^1];
        var segmentDuration = policy.Window.Ticks / policy.SegmentsPerWindow;
        var nextAvailableTime = oldestSegment + policy.Window.Ticks;
        var now = DateTimeOffset.UtcNow.Ticks;
        
        if (nextAvailableTime > now)
        {
            return TimeSpan.FromTicks(nextAvailableTime - now);
        }
        
        return TimeSpan.FromTicks(segmentDuration);
    }

    private string GetEndpointIdentifier(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            return endpoint.DisplayName ?? context.Request.Path.Value ?? "unknown";
        }
        
        return $"{context.Request.Method}:{context.Request.Path.Value}";
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get authenticated user ID first
        var userId = context.User?.FindFirst("sub")?.Value ?? 
                    context.User?.FindFirst("id")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ipAddress = GetClientIpAddress(context);
        return $"ip:{ipAddress}";
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers first
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private RateLimitPolicy? GetRateLimitPolicy(string endpoint)
    {
        // Check for specific endpoint policies first
        if (_config.RateLimit.Policies.TryGetValue(endpoint, out var specificPolicy))
        {
            return specificPolicy;
        }

        // Check for pattern-based policies
        foreach (var kvp in _config.RateLimit.Policies)
        {
            if (RedisKeyBuilder.MatchesPattern(endpoint, kvp.Key))
            {
                return kvp.Value;
            }
        }

        // Return default policy for hot endpoints
        if (IsHotEndpoint(endpoint))
        {
            return new RateLimitPolicy
            {
                PermitLimit = _config.RateLimit.DefaultPermitLimit,
                Window = _config.RateLimit.DefaultWindow,
                SegmentsPerWindow = _config.RateLimit.SegmentsPerWindow
            };
        }

        return null; // No rate limiting
    }

    private static bool IsHotEndpoint(string endpoint)
    {
        var hotEndpoints = new[]
        {
            "/api/bookings",
            "/api/flights/search",
            "/api/seats/availability",
            "/api/pricing/calculate"
        };

        return hotEndpoints.Any(hot => endpoint.Contains(hot, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = result.ResetTime.ToUnixTimeSeconds().ToString();
        
        if (result.RetryAfter.HasValue)
        {
            context.Response.Headers["Retry-After"] = ((int)result.RetryAfter.Value.TotalSeconds).ToString();
        }
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitResult result)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Limit: {result.Limit} per window.",
            limit = result.Limit,
            remaining = result.Remaining,
            resetTime = result.ResetTime,
            retryAfter = result.RetryAfter?.TotalSeconds
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);

        _logger.LogWarning("Rate limit exceeded for endpoint {Endpoint}, client {ClientId}. Limit: {Limit}, Remaining: {Remaining}",
            GetEndpointIdentifier(context), GetClientIdentifier(context), result.Limit, result.Remaining);
    }
}

/// <summary>
/// Rate limit result
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTimeOffset ResetTime { get; set; }
    public TimeSpan? RetryAfter { get; set; }
}

/// <summary>
/// Rate limit options for configuration
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimit";
    
    public bool Enabled { get; set; } = true;
    public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new();
    public RateLimitPolicy DefaultPolicy { get; set; } = new()
    {
        PermitLimit = 100,
        Window = TimeSpan.FromMinutes(1),
        SegmentsPerWindow = 8
    };
}

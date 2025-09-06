namespace FlightBooking.Api.Middleware;

public class GuestIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GuestIdMiddleware> _logger;
    private const string GuestIdCookieName = "guest_id";
    private const string GuestIdHeaderName = "X-Guest-Id";

    public GuestIdMiddleware(RequestDelegate next, ILogger<GuestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var guestId = GetOrCreateGuestId(context);
        
        // Add guest ID to response header for client reference
        context.Response.Headers[GuestIdHeaderName] = guestId;
        
        // Add guest ID to HttpContext items for use in controllers
        context.Items["GuestId"] = guestId;

        await _next(context);
    }

    private string GetOrCreateGuestId(HttpContext context)
    {
        // Try to get existing guest ID from cookie
        if (context.Request.Cookies.TryGetValue(GuestIdCookieName, out var existingGuestId) &&
            IsValidGuestId(existingGuestId))
        {
            return existingGuestId;
        }

        // Try to get guest ID from header (for API clients)
        if (context.Request.Headers.TryGetValue(GuestIdHeaderName, out var headerGuestId) &&
            IsValidGuestId(headerGuestId.ToString()))
        {
            // Set cookie for future requests
            SetGuestIdCookie(context, headerGuestId.ToString());
            return headerGuestId.ToString();
        }

        // Create new guest ID
        var newGuestId = GenerateGuestId();
        SetGuestIdCookie(context, newGuestId);
        
        _logger.LogDebug("Created new guest ID: {GuestId}", newGuestId);
        return newGuestId;
    }

    private static string GenerateGuestId()
    {
        return $"guest_{Guid.NewGuid():N}";
    }

    private static bool IsValidGuestId(string? guestId)
    {
        if (string.IsNullOrEmpty(guestId))
            return false;

        return guestId.StartsWith("guest_") && 
               guestId.Length == 38 && // "guest_" + 32 char GUID
               Guid.TryParse(guestId[6..], out _);
    }

    private static void SetGuestIdCookie(HttpContext context, string guestId)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30), // 30 days expiry
            Path = "/"
        };

        context.Response.Cookies.Append(GuestIdCookieName, guestId, cookieOptions);
    }
}

// Extension method for easy access to guest ID
public static class HttpContextExtensions
{
    public static string? GetGuestId(this HttpContext context)
    {
        return context.Items["GuestId"] as string;
    }
}

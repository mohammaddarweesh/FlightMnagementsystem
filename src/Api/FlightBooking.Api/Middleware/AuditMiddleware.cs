using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FlightBooking.Domain.Audit;
using FlightBooking.Infrastructure.Data;
using Microsoft.Extensions.Primitives;

namespace FlightBooking.Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HashSet<string> _excludedPaths;
    private readonly HashSet<string> _sensitiveHeaders;

    public AuditMiddleware(
        RequestDelegate next,
        ILogger<AuditMiddleware> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        
        // Paths to exclude from auditing
        _excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/health",
            "/swagger",
            "/favicon.ico",
            "/_framework",
            "/css",
            "/js",
            "/images"
        };

        // Headers to exclude from logging (PII/Security)
        _sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-API-Key",
            "X-Auth-Token"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auditing for excluded paths
        if (ShouldSkipAudit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var correlationId = GetOrCreateCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Capture request details
            var requestBody = await CaptureRequestBodyAsync(context.Request);
            
            // Create response capture stream
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            // Execute the request
            await _next(context);

            stopwatch.Stop();

            // Capture response details
            var responseBody = await CaptureResponseBodyAsync(responseBodyStream, originalBodyStream);

            // Create audit entry
            await CreateAuditEntryAsync(context, correlationId, stopwatch.ElapsedMilliseconds, requestBody, responseBody);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Log the exception and create audit entry for failed request
            _logger.LogError(ex, "Request failed for {Path}", context.Request.Path);
            await CreateAuditEntryAsync(context, correlationId, stopwatch.ElapsedMilliseconds, null, null, ex);
            
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldSkipAudit(PathString path)
    {
        return _excludedPaths.Any(excluded => path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from header
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) && 
            !StringValues.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString("N")[..12]; // 12 character short ID
        context.Request.Headers["X-Correlation-ID"] = newCorrelationId;
        context.Response.Headers["X-Correlation-ID"] = newCorrelationId;
        
        return newCorrelationId;
    }

    private async Task<string?> CaptureRequestBodyAsync(HttpRequest request)
    {
        if (!ShouldCaptureBody(request.ContentType, request.ContentLength))
            return null;

        try
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength ?? 0)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Position = 0;

            var body = Encoding.UTF8.GetString(buffer);
            return SanitizeRequestBody(body, request.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture request body");
            return "[Error capturing request body]";
        }
    }

    private async Task<string?> CaptureResponseBodyAsync(MemoryStream responseBodyStream, Stream originalBodyStream)
    {
        try
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);

            return SanitizeResponseBody(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture response body");
            return "[Error capturing response body]";
        }
    }

    private bool ShouldCaptureBody(string? contentType, long? contentLength)
    {
        // Don't capture large bodies or binary content
        if (contentLength > 10 * 1024) // 10KB limit
            return false;

        if (string.IsNullOrEmpty(contentType))
            return false;

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("application/xml", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("text/", StringComparison.OrdinalIgnoreCase);
    }

    private string? SanitizeRequestBody(string body, string? contentType)
    {
        if (string.IsNullOrEmpty(body))
            return null;

        // For JSON, remove sensitive fields
        if (contentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            return SanitizeJsonBody(body);
        }

        return body.Length > 1000 ? body[..1000] + "..." : body;
    }

    private string? SanitizeResponseBody(string body)
    {
        if (string.IsNullOrEmpty(body))
            return null;

        // Limit response body size
        return body.Length > 1000 ? body[..1000] + "..." : body;
    }

    private string SanitizeJsonBody(string jsonBody)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonBody);
            var sanitized = SanitizeJsonElement(jsonDoc.RootElement);
            return JsonSerializer.Serialize(sanitized);
        }
        catch
        {
            return jsonBody.Length > 1000 ? jsonBody[..1000] + "..." : jsonBody;
        }
    }

    private object SanitizeJsonElement(JsonElement element)
    {
        var sensitiveFields = new[] { "password", "confirmPassword", "currentPassword", "newPassword", "token", "apiKey", "secret" };

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    if (sensitiveFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        obj[property.Name] = "[REDACTED]";
                    }
                    else
                    {
                        obj[property.Name] = SanitizeJsonElement(property.Value);
                    }
                }
                return obj;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(SanitizeJsonElement).ToArray();

            case JsonValueKind.String:
                return element.GetString() ?? "";

            case JsonValueKind.Number:
                return element.GetDecimal();

            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();

            case JsonValueKind.Null:
                return null!;

            default:
                return element.ToString();
        }
    }

    private async Task CreateAuditEntryAsync(
        HttpContext context, 
        string correlationId, 
        long latencyMs, 
        string? requestBody, 
        string? responseBody, 
        Exception? exception = null)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var auditEntry = CreateAuditOutboxEntry(context, correlationId, latencyMs, requestBody, responseBody, exception);
            
            dbContext.AuditOutbox.Add(auditEntry);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit entry for correlation ID: {CorrelationId}", correlationId);
        }
    }

    private AuditOutbox CreateAuditOutboxEntry(
        HttpContext context, 
        string correlationId, 
        long latencyMs, 
        string? requestBody, 
        string? responseBody, 
        Exception? exception)
    {
        var userId = GetUserId(context);
        var guestId = GetGuestId(context);
        var userEmail = GetUserEmail(context);
        var userRoles = GetUserRoles(context);

        var auditEntry = new AuditOutbox
        {
            CorrelationId = correlationId,
            UserId = userId,
            GuestId = guestId,
            Route = context.Request.Path,
            HttpMethod = context.Request.Method,
            IpAddress = GetClientIpAddress(context),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            StatusCode = context.Response.StatusCode,
            LatencyMs = latencyMs,
            RequestBody = requestBody,
            ResponseBody = responseBody,
            UserEmail = userEmail,
            UserRoles = userRoles,
            RequestSize = context.Request.ContentLength,
            ResponseSize = context.Response.ContentLength,
            Headers = GetSanitizedHeaders(context.Request.Headers),
            QueryParameters = context.Request.QueryString.ToString(),
            Timestamp = DateTime.UtcNow
        };

        if (exception != null)
        {
            auditEntry.ErrorMessage = exception.Message;
            auditEntry.ResultSummary = $"Exception: {exception.GetType().Name}";
        }
        else
        {
            auditEntry.ResultSummary = GetResultSummary(context.Response.StatusCode, context.Request.Path);
        }

        return auditEntry;
    }

    private Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetGuestId(HttpContext context)
    {
        return context.GetGuestId();
    }

    private string? GetUserEmail(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    private string? GetUserRoles(HttpContext context)
    {
        var roles = context.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value);
        return roles?.Any() == true ? string.Join(",", roles) : null;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetSanitizedHeaders(IHeaderDictionary headers)
    {
        var sanitized = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            if (_sensitiveHeaders.Contains(header.Key))
            {
                sanitized[header.Key] = "[REDACTED]";
            }
            else
            {
                sanitized[header.Key] = header.Value.ToString();
            }
        }

        return JsonSerializer.Serialize(sanitized);
    }

    private string GetResultSummary(int statusCode, string path)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "Success",
            >= 300 and < 400 => "Redirect",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            >= 400 and < 500 => "Client Error",
            >= 500 => "Server Error",
            _ => "Unknown"
        };
    }
}

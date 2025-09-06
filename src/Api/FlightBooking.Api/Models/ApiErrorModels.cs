using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FlightBooking.Api.Models;

/// <summary>
/// Standard error response model following RFC 7807 Problem Details specification
/// </summary>
/// <remarks>
/// This model provides a standardized way to carry machine-readable details of errors
/// in HTTP response bodies. It includes both human-readable and machine-readable information.
/// </remarks>
/// <example>
/// {
///   "type": "https://example.com/probs/validation-error",
///   "title": "Validation Error",
///   "status": 400,
///   "detail": "The request contains invalid data",
///   "instance": "/api/analytics/revenue",
///   "traceId": "0HN7SPBVKQAAA:00000001",
///   "errors": {
///     "startDate": ["Start date cannot be in the future"],
///     "endDate": ["End date must be after start date"]
///   }
/// }
/// </example>
public class ApiErrorResponse
{
    /// <summary>
    /// A URI reference that identifies the problem type
    /// </summary>
    /// <example>https://example.com/probs/validation-error</example>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    /// <example>Validation Error</example>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The HTTP status code for this occurrence of the problem
    /// </summary>
    /// <example>400</example>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    /// <example>The request contains invalid data</example>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem
    /// </summary>
    /// <example>/api/analytics/revenue</example>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    /// <summary>
    /// Unique identifier for tracing the request
    /// </summary>
    /// <example>0HN7SPBVKQAAA:00000001</example>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Additional error details, typically validation errors
    /// </summary>
    /// <example>
    /// {
    ///   "startDate": ["Start date cannot be in the future"],
    ///   "endDate": ["End date must be after start date"]
    /// }
    /// </example>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Additional extension members for problem-specific information
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; set; }
}

/// <summary>
/// Validation error response model for input validation failures
/// </summary>
/// <example>
/// {
///   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
///   "title": "One or more validation errors occurred.",
///   "status": 400,
///   "errors": {
///     "startDate": ["The startDate field is required."],
///     "endDate": ["The endDate field must be a valid date."]
///   }
/// }
/// </example>
public class ValidationErrorResponse : ApiErrorResponse
{
    /// <summary>
    /// Validation errors grouped by field name
    /// </summary>
    /// <example>
    /// {
    ///   "startDate": ["The startDate field is required."],
    ///   "endDate": ["The endDate field must be a valid date."]
    /// }
    /// </example>
    [JsonPropertyName("errors")]
    public new Dictionary<string, string[]> Errors { get; set; } = new();
}

/// <summary>
/// Authentication error response model
/// </summary>
/// <example>
/// {
///   "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
///   "title": "Unauthorized",
///   "status": 401,
///   "detail": "Authentication is required to access this resource"
/// }
/// </example>
public class AuthenticationErrorResponse : ApiErrorResponse
{
    /// <summary>
    /// Creates a new authentication error response
    /// </summary>
    public AuthenticationErrorResponse()
    {
        Type = "https://tools.ietf.org/html/rfc7235#section-3.1";
        Title = "Unauthorized";
        Status = 401;
        Detail = "Authentication is required to access this resource";
    }
}

/// <summary>
/// Authorization error response model
/// </summary>
/// <example>
/// {
///   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
///   "title": "Forbidden",
///   "status": 403,
///   "detail": "You do not have permission to access this resource"
/// }
/// </example>
public class AuthorizationErrorResponse : ApiErrorResponse
{
    /// <summary>
    /// Creates a new authorization error response
    /// </summary>
    public AuthorizationErrorResponse()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
        Title = "Forbidden";
        Status = 403;
        Detail = "You do not have permission to access this resource";
    }
}

/// <summary>
/// Not found error response model
/// </summary>
/// <example>
/// {
///   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
///   "title": "Not Found",
///   "status": 404,
///   "detail": "The requested resource was not found"
/// }
/// </example>
public class NotFoundErrorResponse : ApiErrorResponse
{
    /// <summary>
    /// Creates a new not found error response
    /// </summary>
    public NotFoundErrorResponse()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        Title = "Not Found";
        Status = 404;
        Detail = "The requested resource was not found";
    }
}

/// <summary>
/// Internal server error response model
/// </summary>
/// <example>
/// {
///   "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
///   "title": "Internal Server Error",
///   "status": 500,
///   "detail": "An unexpected error occurred while processing the request"
/// }
/// </example>
public class InternalServerErrorResponse : ApiErrorResponse
{
    /// <summary>
    /// Creates a new internal server error response
    /// </summary>
    public InternalServerErrorResponse()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        Title = "Internal Server Error";
        Status = 500;
        Detail = "An unexpected error occurred while processing the request";
    }
}

/// <summary>
/// Rate limit exceeded error response model
/// </summary>
/// <example>
/// {
///   "type": "https://tools.ietf.org/html/rfc6585#section-4",
///   "title": "Too Many Requests",
///   "status": 429,
///   "detail": "Rate limit exceeded. Please try again later.",
///   "retryAfter": 60
/// }
/// </example>
public class RateLimitErrorResponse : ApiErrorResponse
{
    /// <summary>
    /// Number of seconds to wait before retrying
    /// </summary>
    /// <example>60</example>
    [JsonPropertyName("retryAfter")]
    public int? RetryAfter { get; set; }

    /// <summary>
    /// Creates a new rate limit error response
    /// </summary>
    public RateLimitErrorResponse()
    {
        Type = "https://tools.ietf.org/html/rfc6585#section-4";
        Title = "Too Many Requests";
        Status = 429;
        Detail = "Rate limit exceeded. Please try again later.";
    }
}

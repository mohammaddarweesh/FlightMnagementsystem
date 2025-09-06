namespace FlightBooking.Contracts.Common;

/// <summary>
/// Base response class for all API responses.
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the success message if the operation succeeded.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets additional data returned by the operation.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the response.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

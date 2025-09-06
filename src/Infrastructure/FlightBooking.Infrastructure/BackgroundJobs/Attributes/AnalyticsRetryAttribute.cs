namespace FlightBooking.Infrastructure.BackgroundJobs.Attributes;

/// <summary>
/// Retry attribute specifically for analytics jobs
/// </summary>
public class AnalyticsRetryAttribute : ExponentialBackoffRetryAttribute
{
    public AnalyticsRetryAttribute() : base(
        maxAttempts: 5,
        baseDelaySeconds: 60, // 1 minute
        maxDelaySeconds: 3600, // 1 hour
        backoffMultiplier: 2.0,
        enableJitter: true)
    {
    }
}

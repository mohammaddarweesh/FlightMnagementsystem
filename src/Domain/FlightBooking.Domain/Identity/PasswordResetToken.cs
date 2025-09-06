using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Identity;

public class PasswordResetToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public string? UsedByIp { get; set; }
    public string RequestedByIp { get; set; } = string.Empty;

    // Navigation properties
    public virtual User User { get; set; } = null!;

    // Computed properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    // Helper methods
    public void MarkAsUsed(string? ip = null)
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UsedByIp = ip;
        UpdatedAt = DateTime.UtcNow;
    }

    public static PasswordResetToken Create(Guid userId, string email, string token, string requestedByIp, TimeSpan validFor)
    {
        return new PasswordResetToken
        {
            UserId = userId,
            Email = email,
            Token = token,
            RequestedByIp = requestedByIp,
            ExpiresAt = DateTime.UtcNow.Add(validFor)
        };
    }

    public static PasswordResetToken CreateWithDefaultExpiry(Guid userId, string email, string token, string requestedByIp)
    {
        return Create(userId, email, token, requestedByIp, TimeSpan.FromHours(1)); // 1 hour default
    }
}

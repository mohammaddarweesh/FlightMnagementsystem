using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Identity;

public class EmailVerificationToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public string? UsedByIp { get; set; }

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

    public static EmailVerificationToken Create(Guid userId, string email, string token, TimeSpan validFor)
    {
        return new EmailVerificationToken
        {
            UserId = userId,
            Email = email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(validFor)
        };
    }

    public static EmailVerificationToken CreateWithDefaultExpiry(Guid userId, string email, string token)
    {
        return Create(userId, email, token, TimeSpan.FromHours(24)); // 24 hours default
    }
}

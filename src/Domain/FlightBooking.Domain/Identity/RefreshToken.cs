using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Identity;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public string? UserAgent { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    // Computed properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Helper methods
    public void Revoke(string? ip = null, string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = ip;
        ReplacedByToken = replacedByToken;
        UpdatedAt = DateTime.UtcNow;
    }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt, string createdByIp, string? userAgent = null)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
            UserAgent = userAgent
        };
    }
}

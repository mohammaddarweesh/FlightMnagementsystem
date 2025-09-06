using FlightBooking.Domain.Identity;

namespace FlightBooking.Application.Identity.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string GenerateEmailVerificationToken();
    string GeneratePasswordResetToken();
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string token, string createdByIp, string? userAgent = null);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string? revokedByIp = null, string? replacedByToken = null);
    Task RevokeAllUserRefreshTokensAsync(Guid userId, string? revokedByIp = null);
    Task CleanupExpiredTokensAsync();
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
}

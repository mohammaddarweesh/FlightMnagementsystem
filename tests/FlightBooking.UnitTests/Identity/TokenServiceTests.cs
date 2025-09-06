using FlightBooking.Domain.Identity;
using FlightBooking.Infrastructure.Data;
using FlightBooking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FlightBooking.UnitTests.Identity;

public class TokenServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;
    private readonly Mock<IConfiguration> _configurationMock;

    public TokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["JwtSettings:SecretKey"]).Returns("ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789");
        _configurationMock.Setup(c => c["JwtSettings:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["JwtSettings:Audience"]).Returns("TestAudience");
        _configurationMock.Setup(c => c["JwtSettings:ExpiryMinutes"]).Returns("15");
        _configurationMock.Setup(c => c["JwtSettings:RefreshTokenExpiryDays"]).Returns("7");

        _tokenService = new TokenService(_context, _configurationMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueToken()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateEmailVerificationToken_ReturnsValidToken()
    {
        // Act
        var token = _tokenService.GenerateEmailVerificationToken();

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }

    [Fact]
    public void GeneratePasswordResetToken_ReturnsValidToken()
    {
        // Act
        var token = _tokenService.GeneratePasswordResetToken();

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ValidData_CreatesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "test-refresh-token";
        var ip = "127.0.0.1";
        var userAgent = "Test User Agent";

        // Act
        var refreshToken = await _tokenService.CreateRefreshTokenAsync(userId, token, ip, userAgent);

        // Assert
        Assert.NotNull(refreshToken);
        Assert.Equal(userId, refreshToken.UserId);
        Assert.Equal(token, refreshToken.Token);
        Assert.Equal(ip, refreshToken.CreatedByIp);
        Assert.Equal(userAgent, refreshToken.UserAgent);
        Assert.True(refreshToken.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "valid-refresh-token";
        await _tokenService.CreateRefreshTokenAsync(userId, token, "127.0.0.1");

        // Act
        var isValid = await _tokenService.ValidateRefreshTokenAsync(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_InvalidToken_ReturnsFalse()
    {
        // Act
        var isValid = await _tokenService.ValidateRefreshTokenAsync("invalid-token");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ValidToken_RevokesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "token-to-revoke";
        var ip = "127.0.0.1";
        await _tokenService.CreateRefreshTokenAsync(userId, token, ip);

        // Act
        await _tokenService.RevokeRefreshTokenAsync(token, ip, "replacement-token");

        // Assert
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        Assert.NotNull(refreshToken);
        Assert.True(refreshToken.IsRevoked);
        Assert.Equal(ip, refreshToken.RevokedByIp);
        Assert.Equal("replacement-token", refreshToken.ReplacedByToken);
    }

    [Fact]
    public async Task RevokeAllUserRefreshTokensAsync_ValidUserId_RevokesAllTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ip = "127.0.0.1";
        await _tokenService.CreateRefreshTokenAsync(userId, "token1", ip);
        await _tokenService.CreateRefreshTokenAsync(userId, "token2", ip);

        // Act
        await _tokenService.RevokeAllUserRefreshTokensAsync(userId, ip);

        // Assert
        var userTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();

        Assert.All(userTokens, token => Assert.True(token.IsRevoked));
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_RemovesExpiredTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiredRefreshToken = RefreshToken.Create(userId, "expired-token", DateTime.UtcNow.AddDays(-1), "127.0.0.1");
        var expiredEmailToken = EmailVerificationToken.Create(userId, "test@example.com", "expired-email-token", TimeSpan.FromDays(-1));
        var expiredPasswordToken = PasswordResetToken.Create(userId, "test@example.com", "expired-password-token", "127.0.0.1", TimeSpan.FromDays(-1));

        _context.RefreshTokens.Add(expiredRefreshToken);
        _context.EmailVerificationTokens.Add(expiredEmailToken);
        _context.PasswordResetTokens.Add(expiredPasswordToken);
        await _context.SaveChangesAsync();

        // Act
        await _tokenService.CleanupExpiredTokensAsync();

        // Assert
        var refreshTokenCount = await _context.RefreshTokens.CountAsync();
        var emailTokenCount = await _context.EmailVerificationTokens.CountAsync();
        var passwordTokenCount = await _context.PasswordResetTokens.CountAsync();

        Assert.Equal(0, refreshTokenCount);
        Assert.Equal(0, emailTokenCount);
        Assert.Equal(0, passwordTokenCount);
    }

    private static User CreateTestUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailVerified = true
        };

        // Add a test role
        var role = new Role { Name = "Customer" };
        var userRole = new UserRole { User = user, Role = role };
        user.UserRoles.Add(userRole);

        return user;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

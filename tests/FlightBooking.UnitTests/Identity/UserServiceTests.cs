using FlightBooking.Application.Identity.Interfaces;
using FlightBooking.Domain.Identity;
using FlightBooking.Infrastructure.Data;
using FlightBooking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlightBooking.UnitTests.Identity;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _passwordServiceMock = new Mock<IPasswordService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            _context,
            _passwordServiceMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);

        SeedTestData();
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var user = await _userService.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Act
        var user = await _userService.GetByEmailAsync(email);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var exists = await _userService.EmailExistsAsync(email);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Act
        var exists = await _userService.EmailExistsAsync(email);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task CreateUserAsync_ValidData_CreatesUser()
    {
        // Arrange
        var email = "newuser@example.com";
        var firstName = "New";
        var lastName = "User";
        var password = "Password123!";
        var hashedPassword = "hashed_password";

        _passwordServiceMock.Setup(p => p.IsPasswordStrong(password)).Returns(true);
        _passwordServiceMock.Setup(p => p.HashPassword(password)).Returns(hashedPassword);
        _tokenServiceMock.Setup(t => t.GenerateEmailVerificationToken()).Returns("verification_token");

        // Act
        var user = await _userService.CreateUserAsync(email, firstName, lastName, password);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(email.ToLower(), user.Email);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(hashedPassword, user.PasswordHash);
        Assert.False(user.EmailVerified);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task CreateUserAsync_ExistingEmail_ThrowsException()
    {
        // Arrange
        var email = "test@example.com"; // Already exists
        var firstName = "Test";
        var lastName = "User";
        var password = "Password123!";

        _passwordServiceMock.Setup(p => p.IsPasswordStrong(password)).Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.CreateUserAsync(email, firstName, lastName, password));
    }

    [Fact]
    public async Task CreateUserAsync_WeakPassword_ThrowsException()
    {
        // Arrange
        var email = "newuser@example.com";
        var firstName = "New";
        var lastName = "User";
        var password = "weak";

        _passwordServiceMock.Setup(p => p.IsPasswordStrong(password)).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.CreateUserAsync(email, firstName, lastName, password));
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_VerifiesEmail()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var token = "valid_verification_token";
        var ip = "127.0.0.1";

        var verificationToken = EmailVerificationToken.CreateWithDefaultExpiry(user.Id, user.Email, token);
        _context.EmailVerificationTokens.Add(verificationToken);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.VerifyEmailAsync(token, ip);

        // Assert
        Assert.True(result);
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.True(updatedUser!.EmailVerified);
        Assert.NotNull(updatedUser.EmailVerifiedAt);
    }

    [Fact]
    public async Task VerifyEmailAsync_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var token = "invalid_token";
        var ip = "127.0.0.1";

        // Act
        var result = await _userService.VerifyEmailAsync(token, ip);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_ExistingEmail_SendsResetEmail()
    {
        // Arrange
        var email = "test@example.com";
        var ip = "127.0.0.1";

        _tokenServiceMock.Setup(t => t.GeneratePasswordResetToken()).Returns("reset_token");

        // Act
        var result = await _userService.RequestPasswordResetAsync(email, ip);

        // Assert
        Assert.True(result);
        _emailServiceMock.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_NonExistingEmail_ReturnsTrue()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var ip = "127.0.0.1";

        // Act
        var result = await _userService.RequestPasswordResetAsync(email, ip);

        // Assert
        Assert.True(result); // Should return true to not reveal email existence
        _emailServiceMock.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ResetsPassword()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var token = "valid_reset_token";
        var newPassword = "NewPassword123!";
        var hashedPassword = "new_hashed_password";
        var ip = "127.0.0.1";

        var resetToken = PasswordResetToken.CreateWithDefaultExpiry(user.Id, user.Email, token, ip);
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        _passwordServiceMock.Setup(p => p.IsPasswordStrong(newPassword)).Returns(true);
        _passwordServiceMock.Setup(p => p.HashPassword(newPassword)).Returns(hashedPassword);

        // Act
        var result = await _userService.ResetPasswordAsync(token, newPassword, ip);

        // Assert
        Assert.True(result);
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal(hashedPassword, updatedUser!.PasswordHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_ChangesPassword()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword123!";
        var newHashedPassword = "new_hashed_password";

        _passwordServiceMock.Setup(p => p.VerifyPassword(currentPassword, user.PasswordHash)).Returns(true);
        _passwordServiceMock.Setup(p => p.IsPasswordStrong(newPassword)).Returns(true);
        _passwordServiceMock.Setup(p => p.HashPassword(newPassword)).Returns(newHashedPassword);

        // Act
        var result = await _userService.ChangePasswordAsync(user.Id, currentPassword, newPassword);

        // Assert
        Assert.True(result);
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal(newHashedPassword, updatedUser!.PasswordHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_IncorrectCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var currentPassword = "WrongPassword";
        var newPassword = "NewPassword123!";

        _passwordServiceMock.Setup(p => p.VerifyPassword(currentPassword, user.PasswordHash)).Returns(false);

        // Act
        var result = await _userService.ChangePasswordAsync(user.Id, currentPassword, newPassword);

        // Assert
        Assert.False(result);
    }

    private void SeedTestData()
    {
        var customerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = Role.Names.Customer,
            Description = "Customer role",
            IsActive = true
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed_password",
            EmailVerified = false,
            IsActive = true
        };

        _context.Roles.Add(customerRole);
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    private async Task<User> CreateTestUserAsync()
    {
        var user = new User
        {
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed_password",
            EmailVerified = false,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

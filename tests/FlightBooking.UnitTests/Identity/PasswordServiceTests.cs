using FlightBooking.Infrastructure.Identity;
using Xunit;

namespace FlightBooking.UnitTests.Identity;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordService.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(null!));
    }

    [Fact]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(string.Empty));
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_NullPassword_ReturnsFalse()
    {
        // Arrange
        var hashedPassword = _passwordService.HashPassword("TestPassword123!");

        // Act
        var result = _passwordService.VerifyPassword(null!, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_NullHash_ReturnsFalse()
    {
        // Act
        var result = _passwordService.VerifyPassword("TestPassword123!", null!);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("Password123!", true)]  // Valid: 8+ chars, upper, lower, digit, special
    [InlineData("password123!", false)] // Invalid: no uppercase
    [InlineData("PASSWORD123!", false)] // Invalid: no lowercase
    [InlineData("Password!", false)]    // Invalid: no digit
    [InlineData("Password123", false)]  // Invalid: no special char
    [InlineData("Pass1!", false)]       // Invalid: too short
    [InlineData("", false)]             // Invalid: empty
    public void IsPasswordStrong_VariousPasswords_ReturnsExpectedResult(string password, bool expected)
    {
        // Act
        var result = _passwordService.IsPasswordStrong(password);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateRandomPassword_DefaultLength_ReturnsValidPassword()
    {
        // Act
        var password = _passwordService.GenerateRandomPassword();

        // Assert
        Assert.NotNull(password);
        Assert.Equal(12, password.Length);
        Assert.True(_passwordService.IsPasswordStrong(password));
    }

    [Fact]
    public void GenerateRandomPassword_CustomLength_ReturnsPasswordWithCorrectLength()
    {
        // Arrange
        var length = 16;

        // Act
        var password = _passwordService.GenerateRandomPassword(length);

        // Assert
        Assert.NotNull(password);
        Assert.Equal(length, password.Length);
        Assert.True(_passwordService.IsPasswordStrong(password));
    }

    [Fact]
    public void GenerateRandomPassword_TooShort_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.GenerateRandomPassword(7));
    }

    [Fact]
    public void GenerateRandomPassword_MultipleCalls_ReturnsDifferentPasswords()
    {
        // Act
        var password1 = _passwordService.GenerateRandomPassword();
        var password2 = _passwordService.GenerateRandomPassword();

        // Assert
        Assert.NotEqual(password1, password2);
    }
}

using FlightBooking.Contracts.Common;

namespace FlightBooking.Contracts.Identity;

public class AuthenticationResponse : BaseResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class RefreshTokenResponse : BaseResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfileImageUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RegisterResponse : BaseResponse
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = "Registration successful. Please check your email to verify your account.";
}

public class EmailVerificationResponse : BaseResponse
{
    public string Message { get; set; } = "Email verified successfully.";
}

public class PasswordResetResponse : BaseResponse
{
    public string Message { get; set; } = "Password reset successfully.";
}

public class LogoutResponse : BaseResponse
{
    public string Message { get; set; } = "Logged out successfully.";
}

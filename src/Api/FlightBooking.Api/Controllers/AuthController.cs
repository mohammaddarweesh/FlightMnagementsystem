using System.Security.Claims;
using FlightBooking.Application.Identity.Interfaces;
using FlightBooking.Contracts.Common;
using FlightBooking.Contracts.Identity;
using FlightBooking.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService,
        ITokenService tokenService,
        IPasswordService passwordService,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (await _userService.EmailExistsAsync(request.Email))
            {
                return BadRequest(new RegisterResponse
                {
                    Success = false,
                    ErrorMessage = "Email already exists"
                });
            }

            var user = await _userService.CreateUserAsync(
                request.Email,
                request.FirstName,
                request.LastName,
                request.Password);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            return Ok(new RegisterResponse
            {
                UserId = user.Id,
                Success = true
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new RegisterResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during registration"
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                return Unauthorized(new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password"
                });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = "Account is deactivated"
                });
            }

            // Update last login
            user.UpdateLastLogin();

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshTokenString = _tokenService.GenerateRefreshToken();
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(
                user.Id,
                refreshTokenString,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString());

            var expiresAt = DateTime.UtcNow.AddMinutes(15); // From configuration

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return Ok(new AuthenticationResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                ExpiresAt = expiresAt,
                User = MapToUserDto(user)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new AuthenticationResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during login"
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!await _tokenService.ValidateRefreshTokenAsync(request.RefreshToken))
            {
                return Unauthorized(new RefreshTokenResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid refresh token"
                });
            }

            // Get user from refresh token
            var refreshToken = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);
            if (refreshToken?.User == null)
            {
                return Unauthorized(new RefreshTokenResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid refresh token"
                });
            }

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateAccessToken(refreshToken.User);
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();

            // Revoke old refresh token and create new one
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetClientIpAddress(), newRefreshTokenString);
            await _tokenService.CreateRefreshTokenAsync(
                refreshToken.User.Id,
                newRefreshTokenString,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString());

            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            return Ok(new RefreshTokenResponse
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new RefreshTokenResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during token refresh"
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<LogoutResponse>> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetClientIpAddress());

            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                _logger.LogInformation("User logged out: {UserId}", userId.Value);
            }

            return Ok(new LogoutResponse
            {
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new LogoutResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during logout"
            });
        }
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<EmailVerificationResponse>> VerifyEmail([FromQuery] string token)
    {
        try
        {
            var success = await _userService.VerifyEmailAsync(token, GetClientIpAddress());
            if (!success)
            {
                return BadRequest(new EmailVerificationResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid or expired verification token"
                });
            }

            return Ok(new EmailVerificationResponse
            {
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return StatusCode(500, new EmailVerificationResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during email verification"
            });
        }
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<BaseResponse>> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        try
        {
            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal if email exists
                return Ok(new BaseResponse
                {
                    Success = true,
                    ErrorMessage = "If the email exists, a verification email has been sent"
                });
            }

            if (user.EmailVerified)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Email is already verified"
                });
            }

            await _userService.SendEmailVerificationAsync(user.Id);

            return Ok(new BaseResponse
            {
                Success = true,
                ErrorMessage = "Verification email sent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email");
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while sending verification email"
            });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<BaseResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            await _userService.RequestPasswordResetAsync(request.Email, GetClientIpAddress());

            // Always return success to not reveal if email exists
            return Ok(new BaseResponse
            {
                Success = true,
                ErrorMessage = "If the email exists, a password reset email has been sent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request");
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while processing password reset request"
            });
        }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<PasswordResetResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var success = await _userService.ResetPasswordAsync(request.Token, request.Password, GetClientIpAddress());
            if (!success)
            {
                return BadRequest(new PasswordResetResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid or expired reset token"
                });
            }

            return Ok(new PasswordResetResponse
            {
                Success = true
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new PasswordResetResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new PasswordResetResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during password reset"
            });
        }
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            EmailVerified = user.EmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
            LastLoginAt = user.LastLoginAt,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = user.GetRoles().ToList(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }


}

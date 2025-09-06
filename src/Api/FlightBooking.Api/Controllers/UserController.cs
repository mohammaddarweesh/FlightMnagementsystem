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
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _userService.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "User not found"
                });
            }

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving profile"
            });
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var updatedUser = await _userService.UpdateProfileAsync(
                userId.Value,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.DateOfBirth);

            _logger.LogInformation("Profile updated for user: {UserId}", userId.Value);

            return Ok(MapToUserDto(updatedUser));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new BaseResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while updating profile"
            });
        }
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<BaseResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _userService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            if (!success)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Current password is incorrect"
                });
            }

            _logger.LogInformation("Password changed for user: {UserId}", userId.Value);

            return Ok(new BaseResponse
            {
                Success = true,
                ErrorMessage = "Password changed successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new BaseResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while changing password"
            });
        }
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<string>>> GetUserRoles()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var roles = await _userService.GetUserRolesAsync(userId.Value);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving user roles"
            });
        }
    }

    // Admin-only endpoints
    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BaseResponse>> AssignRole([FromBody] AssignRoleRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _userService.AssignRoleAsync(request.UserId, request.RoleName, currentUserId);
            
            if (!success)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to assign role. User or role may not exist."
                });
            }

            _logger.LogInformation("Role {RoleName} assigned to user {UserId} by {AssignedBy}", 
                request.RoleName, request.UserId, currentUserId);

            return Ok(new BaseResponse
            {
                Success = true,
                ErrorMessage = "Role assigned successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", request.RoleName, request.UserId);
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while assigning role"
            });
        }
    }

    [HttpPost("remove-role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BaseResponse>> RemoveRole([FromBody] RemoveRoleRequest request)
    {
        try
        {
            var success = await _userService.RemoveRoleAsync(request.UserId, request.RoleName);
            
            if (!success)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to remove role. User may not have this role."
                });
            }

            _logger.LogInformation("Role {RoleName} removed from user {UserId} by {RemovedBy}", 
                request.RoleName, request.UserId, GetCurrentUserId());

            return Ok(new BaseResponse
            {
                Success = true,
                ErrorMessage = "Role removed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", request.RoleName, request.UserId);
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while removing role"
            });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<UserDto>> GetUserById(Guid id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new BaseResponse
                {
                    Success = false,
                    ErrorMessage = "User not found"
                });
            }

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user: {UserId}", id);
            return StatusCode(500, new BaseResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving user"
            });
        }
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

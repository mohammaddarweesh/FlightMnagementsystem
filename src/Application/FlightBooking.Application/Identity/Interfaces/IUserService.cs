using FlightBooking.Domain.Identity;

namespace FlightBooking.Application.Identity.Interfaces;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateUserAsync(string email, string firstName, string lastName, string password, string? roleName = null);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> VerifyEmailAsync(string token, string? ip = null);
    Task<bool> SendEmailVerificationAsync(Guid userId);
    Task<bool> RequestPasswordResetAsync(string email, string requestedByIp);
    Task<bool> ResetPasswordAsync(string token, string newPassword, string? ip = null);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<User> UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber = null, DateTime? dateOfBirth = null);
    Task<bool> AssignRoleAsync(Guid userId, string roleName, Guid? assignedBy = null);
    Task<bool> RemoveRoleAsync(Guid userId, string roleName);
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);
}

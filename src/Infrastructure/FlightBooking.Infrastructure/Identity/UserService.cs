using FlightBooking.Application.Identity.Interfaces;
using FlightBooking.Domain.Identity;
using FlightBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<UserService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
    }

    public async Task<User> CreateUserAsync(string email, string firstName, string lastName, string password, string? roleName = null)
    {
        if (await EmailExistsAsync(email))
            throw new InvalidOperationException("Email already exists");

        if (!_passwordService.IsPasswordStrong(password))
            throw new ArgumentException("Password does not meet strength requirements");

        var user = new User
        {
            Email = email.ToLower(),
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = _passwordService.HashPassword(password),
            EmailVerified = false,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assign role if specified
        if (!string.IsNullOrEmpty(roleName))
        {
            await AssignRoleAsync(user.Id, roleName);
        }
        else
        {
            // Default to Customer role
            await AssignRoleAsync(user.Id, Role.Names.Customer);
        }

        // Send email verification
        await SendEmailVerificationAsync(user.Id);

        _logger.LogInformation("User created successfully: {Email}", email);
        return user;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> VerifyEmailAsync(string token, string? ip = null)
    {
        var verificationToken = await _context.EmailVerificationTokens
            .Include(evt => evt.User)
            .FirstOrDefaultAsync(evt => evt.Token == token && evt.IsValid);

        if (verificationToken == null)
            return false;

        verificationToken.MarkAsUsed(ip);
        verificationToken.User.MarkEmailAsVerified();

        await _context.SaveChangesAsync();

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(verificationToken.User.Email, verificationToken.User.FirstName);

        _logger.LogInformation("Email verified for user: {Email}", verificationToken.User.Email);
        return true;
    }

    public async Task<bool> SendEmailVerificationAsync(Guid userId)
    {
        var user = await GetByIdAsync(userId);
        if (user == null || user.EmailVerified)
            return false;

        var token = _tokenService.GenerateEmailVerificationToken();
        var verificationToken = EmailVerificationToken.CreateWithDefaultExpiry(userId, user.Email, token);

        _context.EmailVerificationTokens.Add(verificationToken);
        await _context.SaveChangesAsync();

        await _emailService.SendEmailVerificationAsync(user.Email, user.FirstName, token);

        _logger.LogInformation("Email verification sent to: {Email}", user.Email);
        return true;
    }

    public async Task<bool> RequestPasswordResetAsync(string email, string requestedByIp)
    {
        var user = await GetByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal if email exists or not
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return true; // Return true to not reveal email existence
        }

        var token = _tokenService.GeneratePasswordResetToken();
        var resetToken = PasswordResetToken.CreateWithDefaultExpiry(user.Id, user.Email, token, requestedByIp);

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        await _emailService.SendPasswordResetAsync(user.Email, user.FirstName, token);

        _logger.LogInformation("Password reset requested for: {Email}", email);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, string? ip = null)
    {
        if (!_passwordService.IsPasswordStrong(newPassword))
            throw new ArgumentException("Password does not meet strength requirements");

        var resetToken = await _context.PasswordResetTokens
            .Include(prt => prt.User)
            .FirstOrDefaultAsync(prt => prt.Token == token && prt.IsValid);

        if (resetToken == null)
            return false;

        resetToken.MarkAsUsed(ip);
        resetToken.User.PasswordHash = _passwordService.HashPassword(newPassword);
        resetToken.User.UpdatedAt = DateTime.UtcNow;

        // Revoke all refresh tokens for security
        await _tokenService.RevokeAllUserRefreshTokensAsync(resetToken.User.Id, ip);

        await _context.SaveChangesAsync();

        // Send notification email
        await _emailService.SendPasswordChangedNotificationAsync(resetToken.User.Email, resetToken.User.FirstName);

        _logger.LogInformation("Password reset completed for user: {Email}", resetToken.User.Email);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await GetByIdAsync(userId);
        if (user == null)
            return false;

        if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash))
            return false;

        if (!_passwordService.IsPasswordStrong(newPassword))
            throw new ArgumentException("Password does not meet strength requirements");

        user.PasswordHash = _passwordService.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all refresh tokens for security
        await _tokenService.RevokeAllUserRefreshTokensAsync(userId);

        await _context.SaveChangesAsync();

        // Send notification email
        await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.FirstName);

        _logger.LogInformation("Password changed for user: {Email}", user.Email);
        return true;
    }

    public async Task<User> UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber = null, DateTime? dateOfBirth = null)
    {
        var user = await GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        user.UpdateProfile(firstName, lastName, phoneNumber, dateOfBirth);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile updated for user: {Email}", user.Email);
        return user;
    }

    public async Task<bool> AssignRoleAsync(Guid userId, string roleName, Guid? assignedBy = null)
    {
        var user = await GetByIdAsync(userId);
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive);

        if (user == null || role == null)
            return false;

        // Check if user already has this role
        var existingUserRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

        if (existingUserRole != null)
            return true; // Already has role

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = role.Id,
            AssignedBy = assignedBy,
            AssignedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Role {RoleName} assigned to user: {Email}", roleName, user.Email);
        return true;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string roleName)
    {
        var userRole = await _context.UserRoles
            .Include(ur => ur.Role)
            .Include(ur => ur.User)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role.Name == roleName);

        if (userRole == null)
            return false;

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Role {RoleName} removed from user: {Email}", roleName, userRole.User.Email);
        return true;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }
}

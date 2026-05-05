using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Services;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize("user.manage")]
[EnableRateLimiting("DefaultPolicy")]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UsersController> _logger;
    private readonly IEmailService _emailService;

    public UsersController(IUnitOfWork unitOfWork, ILogger<UsersController> logger, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var users = await _unitOfWork.Users.GetAllAsync();
        var tenantUsers = users.Where(u => u.TenantId == tenantId);

        var userDtos = tenantUsers.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            TokenVersion = u.TokenVersion,
            CreatedAt = DateTime.UtcNow // We don't have CreatedAt in User entity yet
        });

        return Ok(userDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null || user.TenantId != tenantId)
        {
            return NotFound();
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            TokenVersion = user.TokenVersion,
            CreatedAt = DateTime.UtcNow
        });
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        // Check if email already exists in the same tenant
        var existingUsers = await _unitOfWork.Users.GetAllAsync();
        var existingUser = existingUsers.FirstOrDefault(u => u.Email == request.Email && u.TenantId == tenantId);
        if (existingUser != null)
        {
            return Conflict(new { message = "User with this email already exists in your tenant" });
        }

        // Create new user with email verification required
        var user = new User
        {
            TenantId = tenantId,
            Name = request.Name,
            Email = request.Email,
            PasswordHash = null, // Will be set when user verifies email
            Role = request.Role ?? "User",
            TokenVersion = 0,
            IsEmailVerified = false,
            EmailVerificationToken = GenerateSecureToken(),
            EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Assign role to user
        var roles = await _unitOfWork.Roles.GetAllAsync();
        var role = roles.FirstOrDefault(r => r.TenantId == tenantId && r.Name == user.Role);
        if (role != null)
        {
            var userRole = new UserRole
            {
                TenantId = tenantId,
                UserId = user.Id,
                RoleId = role.Id
            };
            await _unitOfWork.UserRoles.AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();
        }

        // Send password set email
        try
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            await _emailService.SendPasswordSetEmailAsync(user.Email, user.EmailVerificationToken, user.Name, tenant?.Name);
            _logger.LogInformation("Password set email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password set email to {Email}", user.Email);
            // Don't fail user creation if email fails, but log the error
        }

        _logger.LogInformation("User {Email} created in tenant {TenantId} with role {Role}", 
            user.Email, tenantId, user.Role);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            TokenVersion = user.TokenVersion,
            CreatedAt = DateTime.UtcNow
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null || user.TenantId != tenantId)
        {
            return NotFound();
        }

        // Update user properties
        if (!string.IsNullOrEmpty(request.Name) && request.Name != user.Name)
        {
            user.Name = request.Name;
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            // Check if new email already exists in the same tenant
            var existingUsers = await _unitOfWork.Users.GetAllAsync();
            var emailExists = existingUsers.Any(u => u.Email == request.Email && u.TenantId == tenantId && u.Id != id);
            if (emailExists)
            {
                return Conflict(new { message = "User with this email already exists in your tenant" });
            }
            user.Email = request.Email;
        }

        if (!string.IsNullOrEmpty(request.Role) && request.Role != user.Role)
        {
            // Update role
            var oldRole = user.Role;
            user.Role = request.Role;

            // Update user role assignment
            var userRoles = await _unitOfWork.UserRoles.GetAllAsync();
            var existingUserRole = userRoles.FirstOrDefault(ur => ur.TenantId == tenantId && ur.UserId == id);
            
            if (existingUserRole != null)
            {
                await _unitOfWork.UserRoles.DeleteAsync(existingUserRole);
            }

            var roles = await _unitOfWork.Roles.GetAllAsync();
            var newRole = roles.FirstOrDefault(r => r.TenantId == tenantId && r.Name == request.Role);
            if (newRole != null)
            {
                var newUserRole = new UserRole
                {
                    TenantId = tenantId,
                    UserId = id,
                    RoleId = newRole.Id
                };
                await _unitOfWork.UserRoles.AddAsync(newUserRole);
            }

            _logger.LogInformation("User {UserId} role changed from {OldRole} to {NewRole} in tenant {TenantId}", 
                id, oldRole, request.Role, tenantId);
        }

        await _unitOfWork.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            TokenVersion = user.TokenVersion,
            CreatedAt = DateTime.UtcNow
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null || user.TenantId != tenantId)
        {
            return NotFound();
        }

        // Prevent self-deletion
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (int.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == id)
        {
            return BadRequest(new { message = "You cannot delete your own account" });
        }

        await _unitOfWork.Users.DeleteAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {Email} deleted from tenant {TenantId}", user.Email, tenantId);

        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult> ResetUserPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null || user.TenantId != tenantId)
        {
            return NotFound();
        }

        // Update password
        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        user.TokenVersion++; // Invalidate all existing tokens for this user

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Password reset for user {Email} in tenant {TenantId}", user.Email, tenantId);

        return Ok(new { message = "Password reset successfully" });
    }

    private static string GenerateSecureToken()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}

public record UserDto
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string Role { get; init; }
    public int TokenVersion { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateUserRequest
{
    public string Name { get; init; }
    public string Email { get; init; }
    public string Role { get; init; } = "User";
}

public record UpdateUserRequest
{
    public string Name { get; init; }
    public string Email { get; init; }
    public string Role { get; init; }
}

public record ResetPasswordRequest
{
    public string NewPassword { get; init; }
}

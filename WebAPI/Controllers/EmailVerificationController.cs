using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Services;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailVerificationController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailVerificationController> _logger;

    public EmailVerificationController(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<EmailVerificationController> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("resend")]
    public async Task<ActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequest request)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == request.Email);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (user.IsEmailVerified)
        {
            return BadRequest(new { message = "Email is already verified" });
        }

        // Generate new verification token
        user.EmailVerificationToken = GenerateSecureToken();
        user.EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24);

        await _unitOfWork.SaveChangesAsync();

        // Send verification email
        await _emailService.SendEmailVerificationAsync(user.Email, user.EmailVerificationToken);

        _logger.LogInformation("Verification email resent to {Email}", user.Email);

        return Ok(new { message = "Verification email sent successfully" });
    }

    [HttpPost("verify")]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == request.Email);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (user.IsEmailVerified)
        {
            return BadRequest(new { message = "Email is already verified" });
        }

        if (user.EmailVerificationToken != request.Token || 
            user.EmailVerificationTokenExpires < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Invalid or expired verification token" });
        }

        // Mark email as verified
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpires = null;

        await _unitOfWork.SaveChangesAsync();

        // Send welcome email
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantId);
        await _emailService.SendWelcomeEmailAsync(user.Email, tenant?.Name ?? "SaaSify");

        _logger.LogInformation("Email verified for user {Email}", user.Email);

        return Ok(new { message = "Email verified successfully" });
    }

    [HttpPost("set-password")]
    public async Task<ActionResult> SetPasswordFromEmail([FromBody] SetPasswordRequest request)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == request.Email);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (user.EmailVerificationToken != request.Token || 
            user.EmailVerificationTokenExpires < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Invalid or expired token" });
        }

        // Set password and mark email as verified
        user.PasswordHash = PasswordHasher.HashPassword(request.Password);
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpires = null;

        await _unitOfWork.SaveChangesAsync();

        // Send welcome email
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantId);
        await _emailService.SendWelcomeEmailAsync(user.Email, tenant?.Name ?? "SaaSify");

        _logger.LogInformation("Password set and email verified for user {Email}", user.Email);

        return Ok(new { message = "Password set successfully" });
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}

public record ResendVerificationRequest
{
    public string Email { get; init; }
}

public record VerifyEmailRequest
{
    public string Email { get; init; }
    public string Token { get; init; }
}

public record SetPasswordRequest
{
    public string Email { get; init; }
    public string Token { get; init; }
    public string Password { get; init; }
}

using Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration configuration, ApplicationDbContext context, IEmailService emailService, ILogger<AuthService> logger)
    {
        _configuration = configuration;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return AuthResult.Unauthorized("Invalid email, password, or tenant");

        if (!user.IsEmailVerified)
            return AuthResult.Unauthorized("Please verify your email before logging in");

        return AuthResult.Success(await CreateAuthResponseAsync(user));
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var emailExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
            return AuthResult.Conflict("Email is already registered");

        var tenant = new Tenant { Name = request.TenantName };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var user = new User
        {
            TenantId = tenant.Id,
            Name = request.Name,
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = "Admin",
            Tenant = tenant,
            EmailVerificationToken = GenerateSecureToken(),
            EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24),
            IsEmailVerified = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Send verification email
        try
        {
            await _emailService.SendEmailVerificationAsync(user.Email, user.EmailVerificationToken, user.Name, tenant.Name);
            _logger.LogInformation("Verification email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            // Don't fail registration if email fails, but log the error
        }

        return AuthResult.Success(new AuthResponse(
            tenant.Id,
            user.Id,
            user.Email,
            user.Role,
            null, // Token - Don't provide token until email is verified
            null, // RefreshToken
            DateTime.UtcNow, // AccessTokenExpiresAt
            tenant.Name // TenantName
        ));
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var refreshTokenHash = RefreshTokenService.HashRefreshToken(refreshToken);

        var storedToken = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash);

        if (storedToken == null)
            return AuthResult.Unauthorized("Invalid refresh token");

        if (storedToken.RevokedAt != null)
        {
            storedToken.User.TokenVersion++;
            await _context.SaveChangesAsync();

            return AuthResult.Unauthorized("Token reuse detected");
        }

        if (!storedToken.IsActive)
            return AuthResult.Unauthorized("Invalid refresh token");

        storedToken.RevokedAt = DateTime.UtcNow;

        return AuthResult.Success(
            await CreateAuthResponseAsync(storedToken.User, storedToken)
        );
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var refreshTokenHash = RefreshTokenService.HashRefreshToken(refreshToken);

        var storedToken = await _context.RefreshTokens
                        .IgnoreQueryFilters()
                        .Include(rt => rt.User)
                        .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash);

        if (storedToken == null)
            return;

        if (storedToken.RevokedAt == null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.User.TokenVersion++;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, RefreshToken previousRefreshToken = null)
    {
        // Fetch user permissions for JWT claims
        var permissions = await _context.UserRoles
            .IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id && ur.TenantId == user.TenantId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        var accessToken = GenerateToken(user.Id, user.Email, user.TenantId, user.TokenVersion, permissions, user.IsEmailVerified);
        var refreshToken = RefreshTokenService.GenerateRefreshToken();
        var refreshTokenHash = RefreshTokenService.HashRefreshToken(refreshToken);

        if (previousRefreshToken != null)
            previousRefreshToken.ReplacedByTokenHash = refreshTokenHash;

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays())
        });

        await _context.SaveChangesAsync();

        // Fetch tenant name for the response
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        return new AuthResponse(
            user.TenantId,
            user.Id,
            user.Email,
            user.Role,
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
            tenant?.Name ?? "Unknown"
        );
    }

    private string GenerateToken(int userId, string email, int tenantId, int tokenVersion, List<string> permissions, bool isEmailVerified)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        Console.WriteLine("GEN KEY: " + jwtSettings["SecretKey"]);
        return JwtTokenGenerator.GenerateToken(
            userId,
            email,
            tenantId,
            tokenVersion,
            permissions,
            isEmailVerified,
            jwtSettings["SecretKey"] ?? string.Empty,
            jwtSettings["Issuer"],
            jwtSettings["Audience"],
            GetAccessTokenExpiryMinutes());
    }

    private int GetAccessTokenExpiryMinutes()
    {
        var expiryMinutes = _configuration["JwtSettings:ExpiryMinutes"];
        return int.TryParse(expiryMinutes, out var parsed) ? parsed : 60;
    }

    private int GetRefreshTokenExpiryDays()
    {
        var expiryDays = _configuration["JwtSettings:RefreshTokenExpiryDays"];
        return int.TryParse(expiryDays, out var parsed) ? parsed : 7;
    }

    private static string GenerateSecureToken()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}

public class AuthResult
{
    private AuthResult(AuthResultStatus status, string error, AuthResponse response)
    {
        Status = status;
        Error = error;
        Response = response;
    }

    public AuthResultStatus Status { get; }
    public string Error { get; }
    public AuthResponse Response { get; }

    public static AuthResult Success(AuthResponse response) => new(AuthResultStatus.Success, null, response);
    public static AuthResult Unauthorized(string error) => new(AuthResultStatus.Unauthorized, error, null);
    public static AuthResult Conflict(string error) => new(AuthResultStatus.Conflict, error, null);
}

public enum AuthResultStatus
{
    Success,
    Unauthorized,
    Conflict
}

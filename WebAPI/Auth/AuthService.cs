using Domain.Entities;
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

    public AuthService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == request.TenantId);

        if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return AuthResult.Unauthorized("Invalid email, password, or tenant");

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
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = "Admin",
            Tenant = tenant
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return AuthResult.Success(await CreateAuthResponseAsync(user));
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
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash);

        if (storedToken == null)
            return;

        storedToken.RevokedAt ??= DateTime.UtcNow;
        storedToken.User.TokenVersion++;

        await _context.SaveChangesAsync();
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, RefreshToken previousRefreshToken = null)
    {
        var accessToken = GenerateToken(user.Id, user.Email, user.TenantId, user.TokenVersion);
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

        return new AuthResponse(
            user.TenantId,
            user.Id,
            user.Email,
            user.Role,
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()));
    }

    private string GenerateToken(int userId, string email, int tenantId, int tokenVersion)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        return JwtTokenGenerator.GenerateToken(
            userId,
            email,
            tenantId,
            tokenVersion,
            jwtSettings["SecretKey"],
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

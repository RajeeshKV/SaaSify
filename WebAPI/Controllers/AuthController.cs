using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Email and password are required");

        var result = await _authService.LoginAsync(request);
        return ToActionResult(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantName))
            return BadRequest("Tenant name is required");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required");

        var result = await _authService.RegisterAsync(request);
        return result.Status == AuthResultStatus.Success
            ? CreatedAtAction(nameof(Register), null, result.Response)
            : ToActionResult(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required");

        var result = await _authService.RefreshAsync(request.RefreshToken);
        return ToActionResult(result);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required");

        await _authService.RevokeAsync(request.RefreshToken);
        return NoContent();
    }

    private IActionResult ToActionResult(AuthResult result)
    {
        return result.Status switch
        {
            AuthResultStatus.Success => Ok(result.Response),
            AuthResultStatus.Conflict => Conflict(result.Error),
            AuthResultStatus.Unauthorized => Unauthorized(result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public int TenantId { get; set; }
}

public class RegisterRequest
{
    public string TenantName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; }
}

public record AuthResponse(
    int TenantId,
    int UserId,
    string Email,
    string Role,
    string Token,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);

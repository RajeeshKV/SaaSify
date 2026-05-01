using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // This is a simplified example. In production, validate credentials against database
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Email and password are required");

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var token = JwtTokenGenerator.GenerateToken(
            userId: 1,
            email: request.Email,
            tenantId: request.TenantId,
            secretKey: jwtSettings["SecretKey"],
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            expiryMinutes: int.Parse(jwtSettings["ExpiryMinutes"])
        );

        return Ok(new { token });
    }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public int TenantId { get; set; }
}

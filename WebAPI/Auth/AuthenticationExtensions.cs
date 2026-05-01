using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var key = Encoding.ASCII.GetBytes(secretKey);
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        Console.WriteLine("VAL KEY: " + secretKey);
        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            x.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var db = context.HttpContext.RequestServices
                        .GetRequiredService<ApplicationDbContext>();

                    var userIdClaim = context.Principal
                        .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    var tokenVersionClaim = context.Principal.FindFirst("TokenVersion")?.Value;

                    if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(tokenVersionClaim))
                    {
                        context.Fail("Missing claims");
                        return;
                    }

                    if (!int.TryParse(userIdClaim, out var userId) ||
                        !int.TryParse(tokenVersionClaim, out var tokenVersion))
                    {
                        context.Fail("Invalid claim format");
                        return;
                    }

                    var user = await db.Users
                                .IgnoreQueryFilters()
                                .Where(x => x.Id == userId)
                                .Select(x => new { x.TokenVersion })
                                .FirstOrDefaultAsync();

                    Console.WriteLine($"DB: {user?.TokenVersion}, Token: {tokenVersion}");

                    if (user == null || user.TokenVersion != tokenVersion)
                    {
                        context.Fail("Token revoked");
                    }
                }
            };
        });

        return services;
    }
}

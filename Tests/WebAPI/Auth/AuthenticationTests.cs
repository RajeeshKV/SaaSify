using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Tests.WebAPI.Auth;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_IncludesUserTenantAndEmailClaims()
    {
        var token = JwtTokenGenerator.GenerateToken(
            userId: 1,
            email: "test@example.com",
            tenantId: 1,
            tokenVersion: 1,
            permissions: new List<string> { "project.read", "project.write" },
            secretKey: "test_secret_key",
            issuer: "test_issuer",
            audience: "test_audience",
            expiryMinutes: 60);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("test_issuer");
        jwt.Audiences.Should().Contain("test_audience");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
        jwt.Claims.Should().Contain(c => c.Type == "TenantId" && c.Value == "1");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "5");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "user@example.com");
        jwt.Claims.Should().Contain(c => c.Type == "TenantId" && c.Value == "9");
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }
}

public class AuthenticationExtensionsTests
{
    [Fact]
    public void AddJwtAuthentication_ConfiguresBearerAuthentication()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddJwtAuthentication(configuration);

        using var provider = services.BuildServiceProvider();
        var authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var jwtBearerOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        authenticationOptions.DefaultAuthenticateScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        authenticationOptions.DefaultChallengeScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        jwtBearerOptions.RequireHttpsMetadata.Should().BeFalse();
        jwtBearerOptions.SaveToken.Should().BeTrue();
        jwtBearerOptions.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        jwtBearerOptions.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        jwtBearerOptions.TokenValidationParameters.ValidIssuer.Should().Be("TestIssuer");
        jwtBearerOptions.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        jwtBearerOptions.TokenValidationParameters.ValidAudience.Should().Be("TestAudience");
        jwtBearerOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwtBearerOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
        jwtBearerOptions.TokenValidationParameters.IssuerSigningKey.Should().BeOfType<SymmetricSecurityKey>();
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience"
            })
            .Build();
    }
}

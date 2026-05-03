using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string verificationToken, string userName = null, string tenantName = null);
    Task SendPasswordSetEmailAsync(string email, string setPasswordToken, string userName = null, string tenantName = null);
    Task SendWelcomeEmailAsync(string email, string tenantName);
}

public class BrevoEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _frontendUrl;
    private readonly int _emailVerificationTemplateId;
    private readonly int _userInvitationTemplateId;
    private readonly int _welcomeEmailTemplateId;

    public BrevoEmailService(
        HttpClient httpClient,
        ILogger<BrevoEmailService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Brevo:ApiKey"] ?? throw new ArgumentNullException("Brevo:ApiKey");
        _baseUrl = configuration["Brevo:BaseUrl"] ?? "https://api.brevo.com/v3";
        _frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:3000";
        
        // Get template IDs from configuration
        _emailVerificationTemplateId = int.Parse(configuration["Brevo:Templates:EmailVerification"] ?? throw new ArgumentNullException("Brevo:Templates:EmailVerification"));
        _userInvitationTemplateId = int.Parse(configuration["Brevo:Templates:UserInvitation"] ?? throw new ArgumentNullException("Brevo:Templates:UserInvitation"));
        _welcomeEmailTemplateId = int.Parse(configuration["Brevo:Templates:WelcomeEmail"] ?? throw new ArgumentNullException("Brevo:Templates:WelcomeEmail"));
        
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
    }

    public async Task SendEmailVerificationAsync(string email, string verificationToken, string userName = null, string tenantName = null)
    {
        var verificationUrl = $"{_frontendUrl}/verify-email?token={verificationToken}&email={Uri.EscapeDataString(email)}";
        
        var emailData = new
        {
            templateId = _emailVerificationTemplateId,
            sender = new { name = "SaaSify", email = "noreply@saasify.com" },
            to = new[] { new { email } },
            @params = new
            {
                userName = userName,
                tenantName = tenantName,
                verificationUrl = verificationUrl
            }
        };

        await SendTemplateEmailAsync(emailData);
    }

    public async Task SendPasswordSetEmailAsync(string email, string setPasswordToken, string userName = null, string tenantName = null)
    {
        var setPasswordUrl = $"{_frontendUrl}/set-password?token={setPasswordToken}&email={Uri.EscapeDataString(email)}";
        
        var emailData = new
        {
            templateId = _userInvitationTemplateId,
            sender = new { name = "SaaSify", email = "noreply@saasify.com" },
            to = new[] { new { email } },
            @params = new
            {
                userName = userName,
                tenantName = tenantName,
                setPasswordUrl = setPasswordUrl
            }
        };

        await SendTemplateEmailAsync(emailData);
    }

    public async Task SendWelcomeEmailAsync(string email, string tenantName)
    {
        var loginUrl = $"{_frontendUrl}/login";
        
        var emailData = new
        {
            templateId = _welcomeEmailTemplateId,
            sender = new { name = "SaaSify", email = "noreply@saasify.com" },
            to = new[] { new { email } },
            @params = new
            {
                tenantName = tenantName,
                loginUrl = loginUrl
            }
        };

        await SendTemplateEmailAsync(emailData);
    }

    private async Task SendTemplateEmailAsync(object emailData)
    {
        try
        {
            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/smtp/email", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully using template");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email using template. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to send email: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email using template");
            throw;
        }
    }
}

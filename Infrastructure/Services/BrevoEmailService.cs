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
        _frontendUrl = configuration["Frontend:Url"] ?? "https://saasify.rajeesh.online";
        
        // Get template IDs from configuration
        _emailVerificationTemplateId = int.Parse(configuration["Brevo:Templates:EmailVerification"] ?? throw new ArgumentNullException("Brevo:Templates:EmailVerification"));
        _userInvitationTemplateId = int.Parse(configuration["Brevo:Templates:UserInvitation"] ?? throw new ArgumentNullException("Brevo:Templates:UserInvitation"));
        _welcomeEmailTemplateId = int.Parse(configuration["Brevo:Templates:WelcomeEmail"] ?? throw new ArgumentNullException("Brevo:Templates:WelcomeEmail"));
        
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
        
        // Test API key on startup
        Task.Run(async () => await TestApiKeyAsync());
    }

    private async Task TestApiKeyAsync()
    {
        try
        {
            var testResponse = await _httpClient.GetAsync("/accounts");
            if (testResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Brevo API key is valid and account is accessible");
            }
            else
            {
                _logger.LogWarning("Brevo API key validation failed: {StatusCode}", testResponse.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Brevo API key");
        }
    }

    public async Task SendEmailVerificationAsync(string email, string verificationToken, string userName = null, string tenantName = null)
    {
        var verificationUrl = $"{_frontendUrl}/verify-email?token={verificationToken}&email={Uri.EscapeDataString(email)}";
        
        var emailData = new
        {
            templateId = _emailVerificationTemplateId,
            sender = new { name = "SaaSify", email = "no-reply@rajeesh.online" },
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
            sender = new { name = "SaaSify", email = "no-reply@rajeesh.online" },
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
            sender = new { name = "SaaSify", email = "no-reply@rajeesh.online" },
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
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Template not found, falling back to direct email");
                // Extract email data and send direct email
                await SendDirectEmailAsync(emailData);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email using template. Status: {StatusCode}, Headers: {Headers}, Error: {Error}", 
                    response.StatusCode,
                    string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")),
                    errorContent);
                throw new InvalidOperationException($"Failed to send email: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            var errorContent = ex.Message + Environment.NewLine + ex.StackTrace;
            _logger.LogError("Error sending email using template. Error: {Error}", errorContent);
            throw;
        }
    }

    private async Task SendDirectEmailAsync(object emailData)
    {
        try
        {
            // Parse the email data to extract recipient and content
            var json = JsonSerializer.Serialize(emailData);
            var emailObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (emailObj?.ContainsKey("to") == true && emailObj?.ContainsKey("params") == true)
            {
                var toArray = JsonSerializer.Deserialize<Dictionary<string, string>[]>(emailObj["to"].ToString());
                var recipientEmail = toArray?[0]?["email"] ?? string.Empty;
                
                var paramsObj = JsonSerializer.Deserialize<Dictionary<string, string>>(emailObj["params"].ToString());
                var userName = paramsObj.GetValueOrDefault("userName", "User");
                var setPasswordUrl = paramsObj.GetValueOrDefault("setPasswordUrl", "");
                
                // Create direct email content
                var directEmailData = new
                {
                    sender = new { name = "SaaSify", email = "no-reply@rajeesh.online" },
                    to = new[] { new { email = recipientEmail } },
                    subject = "Set Your Password - SaaSify",
                    htmlContent = $@"
                        <html>
                        <body>
                            <h2>Welcome to SaaSify!</h2>
                            <p>Hello {userName},</p>
                            <p>Your account has been created. Please set your password using the link below:</p>
                            <p><a href='{setPasswordUrl}'>Set Your Password</a></p>
                            <p>If you didn't request this, please ignore this email.</p>
                            <p>Thanks,<br/>SaaSify Team</p>
                        </body>
                        </html>"
                };

                var directJson = JsonSerializer.Serialize(directEmailData);
                var content = new StringContent(directJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/smtp/email", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Direct email sent successfully to {Email}", recipientEmail);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send direct email. Status: {StatusCode}, Headers: {Headers}, Error: {Error}", 
                        response.StatusCode, 
                        string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")),
                        errorContent);
                    throw new InvalidOperationException($"Failed to send direct email: {errorContent}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending direct email");
            throw;
        }
    }
}

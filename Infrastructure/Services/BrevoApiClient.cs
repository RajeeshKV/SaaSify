using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class BrevoApiClient : IExternalApiClient
    {
        private readonly HttpClientProxy _httpClientProxy;
        private readonly ILogger<BrevoApiClient> _logger;

        public BrevoApiClient(
            HttpClientProxy httpClientProxy,
            ILogger<BrevoApiClient> logger)
        {
            _httpClientProxy = httpClientProxy;
            _logger = logger;
        }

        public async Task<EmailResponse> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default)
        {
            return await _httpClientProxy.PostAsync<EmailRequest, EmailResponse>(
                "v3/smtp/email",
                request,
                cancellationToken);
        }

        public async Task<EmailValidationResponse> ValidateApiKeyAsync(CancellationToken cancellationToken = default)
        {
            return await _httpClientProxy.GetAsync<EmailValidationResponse>(
                "v3/account",
                cancellationToken);
        }

        // Implement IExternalApiClient interface methods
        public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class
        {
            return await _httpClientProxy.GetAsync<T>(endpoint, cancellationToken);
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            return await _httpClientProxy.PostAsync<TRequest, TResponse>(endpoint, request, cancellationToken);
        }

        public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            return await _httpClientProxy.PutAsync<TRequest, TResponse>(endpoint, request, cancellationToken);
        }

        public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            await _httpClientProxy.DeleteAsync(endpoint, cancellationToken);
        }
    }

    public record EmailRequest(
        string To,
        string[] Recipients,
        string Subject,
        string HtmlContent,
        string TextContent,
        Dictionary<string, object> Headers = null
    );

    public record EmailResponse(
        string MessageId,
        string Status
    );

    public record EmailValidationResponse(
        string Email,
        string Plan,
        bool IsActive
    );
}

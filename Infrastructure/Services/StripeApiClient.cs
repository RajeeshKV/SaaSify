using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace Infrastructure.Services
{
    public class StripeApiClient : IExternalApiClient
    {
        private readonly HttpClientProxy _httpClientProxy;
        private readonly ILogger<StripeApiClient> _logger;

        public StripeApiClient(
            HttpClientProxy httpClientProxy,
            ILogger<StripeApiClient> logger)
        {
            _httpClientProxy = httpClientProxy;
            _logger = logger;
        }

        public async Task<Session> CreateCheckoutSessionAsync(CheckoutSessionCreateRequest request, CancellationToken cancellationToken = default)
        {
            return await _httpClientProxy.PostAsync<CheckoutSessionCreateRequest, Session>(
                "v1/checkout/sessions",
                request,
                cancellationToken);
        }

        public async Task<Session> RetrieveSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await _httpClientProxy.GetAsync<Session>(
                $"v1/checkout/sessions/{sessionId}",
                cancellationToken);
        }

        public async Task ExpireSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await _httpClientProxy.DeleteAsync(
                $"v1/checkout/sessions/{sessionId}/expire",
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

    public record CheckoutSessionCreateRequest(
        string? CustomerEmail,
        decimal Amount,
        string Currency,
        string SuccessUrl,
        string CancelUrl,
        Dictionary<string, string> Metadata
    );
}

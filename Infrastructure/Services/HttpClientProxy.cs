using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Infrastructure.Services
{
    public class HttpClientProxy : IExternalApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpClientProxy> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IAsyncPolicy _retryPolicy;

        public HttpClientProxy(
            HttpClient httpClient,
            ILogger<HttpClientProxy> logger,
            IOptionsMonitor<HttpClientProxyOptions> optionsMonitor = null)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            // Configure retry policy
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GET {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                return await ProcessResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing POST {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
                return await ProcessResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing PUT {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing DELETE {Endpoint}", endpoint);
                throw;
            }
        }

        private async Task<T> ProcessResponseAsync<T>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, _jsonOptions) ?? throw new JsonException("Failed to deserialize response");
        }
    }

    public class HttpClientProxyOptions
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public int InitialRetryDelaySeconds { get; set; } = 1;
        public double RetryDelayMultiplier { get; set; } = 2;
    }
}

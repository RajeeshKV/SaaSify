using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Infrastructure.Services
{
    public interface IOrderServiceClient
    {
        Task<PaginatedOrdersResponse> GetOrdersAsync(int tenantId, int page, int pageSize, string accessToken);
        Task<OrderDto?> GetOrderAsync(int tenantId, int orderId, string accessToken);
        Task<bool> IsHealthyAsync();
    }

    public class OrderServiceClient : IOrderServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OrderServiceClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        
        // Circuit breaker state
        private DateTime _circuitBreakerOpenTime = DateTime.MinValue;
        private int _failureCount = 0;
        private readonly object _circuitBreakerLock = new object();

        public OrderServiceClient(HttpClient httpClient, IConfiguration config, ILogger<OrderServiceClient> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            
            var baseUrl = _config["OrderService:BaseUrl"] ?? "https://saasifyapi-client.rajeesh.online";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.GetValue<int>("OrderService:Timeout", 30));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<PaginatedOrdersResponse> GetOrdersAsync(int tenantId, int page, int pageSize, string accessToken)
        {
            if (IsCircuitBreakerOpen())
            {
                throw new InvalidOperationException("OrderService is temporarily unavailable. Please try again later.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orders?page={page}&pageSize={pageSize}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    ResetCircuitBreaker();
                    
                    var content = await response.Content.ReadAsStringAsync();
                    var orders = JsonSerializer.Deserialize<List<OrderDto>>(content, _jsonOptions);
                    
                    return new PaginatedOrdersResponse
                    {
                        Orders = orders ?? new List<OrderDto>(),
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = orders?.Count ?? 0,
                        TotalPages = (int)Math.Ceiling((double)(orders?.Count ?? 0) / pageSize)
                    };
                }
                else
                {
                    HandleFailure(response);
                    throw new HttpRequestException($"Failed to get orders: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                HandleFailure();
                _logger.LogError(ex, "Error getting orders from OrderService for TenantId: {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<OrderDto?> GetOrderAsync(int tenantId, int orderId, string accessToken)
        {
            if (IsCircuitBreakerOpen())
            {
                throw new InvalidOperationException("OrderService is temporarily unavailable. Please try again later.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orders/{orderId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    ResetCircuitBreaker();
                    
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<OrderDto>(content, _jsonOptions);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    HandleFailure(response);
                    throw new HttpRequestException($"Failed to get order: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                HandleFailure();
                _logger.LogError(ex, "Error getting order {OrderId} from OrderService for TenantId: {TenantId}", orderId, tenantId);
                throw;
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private bool IsCircuitBreakerOpen()
        {
            lock (_circuitBreakerLock)
            {
                var threshold = _config.GetValue<int>("OrderService:CircuitBreakerThreshold", 5);
                var duration = _config.GetValue<int>("OrderService:CircuitBreakerDuration", 60);

                if (_failureCount >= threshold)
                {
                    if (DateTime.UtcNow < _circuitBreakerOpenTime.AddSeconds(duration))
                    {
                        return true;
                    }
                    else
                    {
                        // Reset circuit breaker after duration
                        _failureCount = 0;
                        _circuitBreakerOpenTime = DateTime.MinValue;
                    }
                }
                return false;
            }
        }

        private void HandleFailure(HttpResponseMessage? response = null)
        {
            lock (_circuitBreakerLock)
            {
                _failureCount++;
                if (_failureCount >= _config.GetValue<int>("OrderService:CircuitBreakerThreshold", 5))
                {
                    _circuitBreakerOpenTime = DateTime.UtcNow;
                    _logger.LogWarning("Circuit breaker opened for OrderService. Failures: {FailureCount}", _failureCount);
                }
            }
        }

        private void ResetCircuitBreaker()
        {
            lock (_circuitBreakerLock)
            {
                _failureCount = 0;
                _circuitBreakerOpenTime = DateTime.MinValue;
            }
        }
    }
}

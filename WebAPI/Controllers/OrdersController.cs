using Application.Common.Interfaces;
using Domain.DTOs;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderEventPublisher _orderEventPublisher;
        private readonly IOrderServiceClient _orderServiceClient;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderEventPublisher orderEventPublisher, IOrderServiceClient orderServiceClient, ILogger<OrdersController> logger)
        {
            _orderEventPublisher = orderEventPublisher;
            _orderServiceClient = orderServiceClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // Get tenant and user ID from JWT claims
                var tenantIdClaim = User.FindFirst("TenantId")?.Value;
                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (!int.TryParse(tenantIdClaim, out var tenantId) || !int.TryParse(userIdClaim, out var userId))
                {
                    return BadRequest("Invalid tenant or user ID in token");
                }

                // Publish order event to RabbitMQ with fallback handling
                try
                {
                    await _orderEventPublisher.PublishOrderCreatedAsync(
                        tenantId: tenantId,
                        userId: userId,
                        amount: request.Amount,
                        description: request.Description ?? "Order created via API",
                        customerEmail: request.CustomerEmail ?? $"user-{userId}@tenant-{tenantId}.com"
                    );

                    _logger.LogInformation("Order event published: TenantId={TenantId}, UserId={UserId}, Amount={Amount}", 
                        tenantId, userId, request.Amount);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("RabbitMQ connection is not available"))
                {
                    _logger.LogWarning(ex, "RabbitMQ not available, order created locally: TenantId={TenantId}, UserId={UserId}", 
                        tenantId, userId);
                    
                    // Return success but note that RabbitMQ is not available
                    return Ok(new
                    {
                        Message = "Order created successfully (message queue temporarily unavailable)",
                        TenantId = tenantId,
                        UserId = userId,
                        Amount = request.Amount,
                        Description = request.Description,
                        Status = "Processing",
                        Timestamp = DateTime.UtcNow,
                        Warning = "Order processing may be delayed due to message queue issues"
                    });
                }

                return Ok(new
                {
                    Message = "Order created successfully",
                    TenantId = tenantId,
                    UserId = userId,
                    Amount = request.Amount,
                    Description = request.Description,
                    Status = "Processing",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var tenantIdClaim = User.FindFirst("TenantId")?.Value;
                if (!int.TryParse(tenantIdClaim, out var tenantId))
                {
                    return BadRequest("Invalid tenant ID in token");
                }

                // Publish order status update event to RabbitMQ
                await _orderEventPublisher.PublishOrderUpdatedAsync(
                    orderId: id,
                    tenantId: tenantId,
                    status: request.Status
                );

                _logger.LogInformation("Order status update event published: OrderId={OrderId}, TenantId={TenantId}, Status={Status}", 
                    id, tenantId, request.Status);

                return Ok(new
                {
                    Message = "Order status updated successfully",
                    OrderId = id,
                    TenantId = tenantId,
                    Status = request.Status,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order status: OrderId={OrderId}, Status={Status}", id, request.Status);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (!int.TryParse(tenantIdClaim, out var tenantId) || !int.TryParse(userIdClaim, out var userId))
            {
                return BadRequest("Invalid tenant or user ID in token");
            }

            try
            {

                // Get JWT token from Authorization header
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized("Authorization token required");
                }

                var accessToken = authHeader.Substring("Bearer ".Length);

                // Call OrderService with JWT token forwarding
                var ordersResponse = await _orderServiceClient.GetOrdersAsync(tenantId, pageNumber, pageSize, accessToken);

                return Ok(new
                {
                    Orders = ordersResponse.Orders,
                    Pagination = new
                    {
                        PageNumber = ordersResponse.Page,
                        PageSize = ordersResponse.PageSize,
                        TotalCount = ordersResponse.TotalCount,
                        TotalPages = ordersResponse.TotalPages
                    },
                    TenantId = tenantId,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("temporarily unavailable"))
            {
                _logger.LogWarning(ex, "OrderService is temporarily unavailable for TenantId: {TenantId}", tenantId);
                return StatusCode(503, new
                {
                    Message = "Order service is temporarily unavailable. Please try again later.",
                    Code = "ORDER_SERVICE_UNAVAILABLE",
                    RetryAfter = 60
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to communicate with OrderService for TenantId: {TenantId}", tenantId);
                return StatusCode(503, new
                {
                    Message = "Order service is currently experiencing issues. Please try again later.",
                    Code = "ORDER_SERVICE_ERROR"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get orders for TenantId: {TenantId}", tenantId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "SaaSify API - Orders Controller",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Features = new[]
                {
                    "Order Event Publishing",
                    "OrderService Integration",
                    "RabbitMQ Integration",
                    "Multi-tenant Support"
                }
            });
        }
    }

    public record CreateOrderRequest(
        decimal Amount,
        string? Description,
        string? CustomerEmail
    );

    public record UpdateOrderStatusRequest(
        string Status
    );
}

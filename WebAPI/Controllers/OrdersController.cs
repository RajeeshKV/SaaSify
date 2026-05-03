using Application.Common.Interfaces;
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
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderEventPublisher orderEventPublisher, ILogger<OrdersController> logger)
        {
            _orderEventPublisher = orderEventPublisher;
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

                // Publish order event to RabbitMQ
                await _orderEventPublisher.PublishOrderCreatedAsync(
                    tenantId: tenantId,
                    userId: userId,
                    amount: request.Amount,
                    description: request.Description ?? "Order created via API",
                    customerEmail: request.CustomerEmail ?? $"user-{userId}@tenant-{tenantId}.com"
                );

                _logger.LogInformation("Order event published: TenantId={TenantId}, UserId={UserId}, Amount={Amount}", 
                    tenantId, userId, request.Amount);

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

using Application.Orders.Commands;
using Application.Orders.Queries;
using Application.Stripe.Commands;
using Application.Stripe.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly CreateOrderCheckoutSessionCommandHandler _createOrderCheckoutSessionHandler;
        private readonly HandleOrderSuccessQueryHandler _handleOrderSuccessQueryHandler;
        private readonly HandleOrderCancelQueryHandler _handleOrderCancelQueryHandler;
        private readonly IConfiguration _configuration;
        private readonly IOrderServiceClient _orderServiceClient;

        public OrdersController(
            CreateOrderCheckoutSessionCommandHandler createOrderCheckoutSessionHandler,
            HandleOrderSuccessQueryHandler handleOrderSuccessQueryHandler,
            HandleOrderCancelQueryHandler handleOrderCancelQueryHandler,
            IConfiguration configuration,
            IOrderServiceClient orderServiceClient)
        {
            _createOrderCheckoutSessionHandler = createOrderCheckoutSessionHandler;
            _handleOrderSuccessQueryHandler = handleOrderSuccessQueryHandler;
            _handleOrderCancelQueryHandler = handleOrderCancelQueryHandler;
            _configuration = configuration;
            _orderServiceClient = orderServiceClient;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateOrderCheckoutSessionRequest request)
        {
            try
            {
                var tenantIdClaim = User.FindFirst("TenantId")?.Value;
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var emailClaim = User.FindFirst("email")?.Value;

                if (!int.TryParse(tenantIdClaim, out var tenantId) || !int.TryParse(userIdClaim, out var userId))
                {
                    return BadRequest("Invalid tenant or user ID in token");
                }

                // Check OrderService availability before processing payment
                var isOrderServiceHealthy = await _orderServiceClient.IsHealthyAsync();
                if (!isOrderServiceHealthy)
                {
                    return StatusCode(503, "Service is temporarily unavailable. Please try again later.");
                }

                var baseUrl = _configuration["BaseUrl"] ?? "http://saasifyapi.rajeesh.online";
                
                var command = new CreateOrderCheckoutSessionCommand
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Amount = request.Amount,
                    CustomerEmail = request.CustomerEmail ?? emailClaim,
                    Currency = request.Currency,
                    OrderId = request.OrderId,
                    Description = request.Description,
                    SuccessUrl = $"{baseUrl}/api/v1/orders/success",
                    CancelUrl = $"{baseUrl}/api/v1/orders/cancel"
                };

                var result = await _createOrderCheckoutSessionHandler.Handle(command, default);

                return Ok(new
                {
                    CheckoutUrl = result.CheckoutUrl,
                    SessionId = result.SessionId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to create checkout session");
            }
        }

        [HttpGet("success")]
        public async Task<IActionResult> Success([FromQuery] string session_id)
        {
            var query = new HandleOrderSuccessQuery { SessionId = session_id };
            var result = await _handleOrderSuccessQueryHandler.Handle(query, default);

            if (result.Success)
            {
                return Redirect(result.RedirectUrl);
            }

            return BadRequest(result.Error);
        }

        [HttpGet("cancel")]
        public async Task<IActionResult> Cancel([FromQuery] string session_id)
        {
            var query = new HandleOrderCancelQuery { SessionId = session_id };
            var result = await _handleOrderCancelQueryHandler.Handle(query, default);

            if (result.Success)
            {
                return Redirect(result.RedirectUrl);
            }

            return BadRequest(result.Error);
        }

        [HttpGet("health")]
        [HttpHead("health")]
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
        string? CustomerEmail,
        string? PaymentIntentId,
        string? PaymentMethod
    );

    public record CreateOrderCheckoutSessionRequest(
        decimal Amount,
        string? CustomerEmail,
        string? Currency,
        int? OrderId,
        string? Description
    );

    public record CreatePaymentIntentRequest(
        decimal Amount,
        string? CustomerEmail,
        string? Currency = "usd"
    );

    public record UpdateOrderStatusRequest(
        string Status
    );
}

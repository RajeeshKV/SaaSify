using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Orders.Commands
{
    public class CreateOrderCheckoutSessionCommand
    {
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Currency { get; set; }
        public int? OrderId { get; set; }
    }

    public class CreateOrderCheckoutSessionCommandHandler
    {
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<CreateOrderCheckoutSessionCommandHandler> _logger;

        public CreateOrderCheckoutSessionCommandHandler(
            IStripePaymentService stripePaymentService,
            ILogger<CreateOrderCheckoutSessionCommandHandler> logger)
        {
            _stripePaymentService = stripePaymentService;
            _logger = logger;
        }

        public async Task<CheckoutSessionResponse> Handle(CreateOrderCheckoutSessionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var checkoutRequest = new CheckoutSessionRequest
                {
                    TenantId = request.TenantId,
                    PlanId = "order", // Use "order" as plan identifier
                    CustomerEmail = request.CustomerEmail,
                    Currency = request.Currency ?? "usd",
                    Amount = request.Amount,
                    SuccessUrl = "", // Will be set by controller
                    CancelUrl = ""  // Will be set by controller
                };

                // Add metadata to checkout session
                checkoutRequest.Metadata["tenant_id"] = request.TenantId.ToString();
                checkoutRequest.Metadata["user_id"] = request.UserId.ToString();
                checkoutRequest.Metadata["customer_email"] = request.CustomerEmail;
                checkoutRequest.Metadata["payment_type"] = "order";
                checkoutRequest.Metadata["order_id"] = request.OrderId?.ToString() ?? "";

                var checkoutSession = await _stripePaymentService.CreateCheckoutSessionAsync(checkoutRequest);

                _logger.LogInformation("Order checkout session created: TenantId={TenantId}, UserId={UserId}, Amount={Amount}, SessionId={SessionId}", 
                    request.TenantId, request.UserId, request.Amount, checkoutSession.SessionId);

                return new CheckoutSessionResponse
                {
                    SessionId = checkoutSession.SessionId,
                    CheckoutUrl = checkoutSession.CheckoutUrl,
                    Status = checkoutSession.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order checkout session for tenant {TenantId}", request.TenantId);
                throw new Exception($"Failed to create order checkout session: {ex.Message}");
            }
        }
    }
}

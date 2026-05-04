using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Stripe.Commands
{
    public class CreateCheckoutSessionCommand
    {
        public int TenantId { get; set; }
        public string PlanId { get; set; }
        public string? CustomerEmail { get; set; }
        public string Currency { get; set; } = "usd";
    }

    public class CreateCheckoutSessionResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    public class CreateCheckoutSessionCommandHandler
    {
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<CreateCheckoutSessionCommandHandler> _logger;

        public CreateCheckoutSessionCommandHandler(
            IStripePaymentService stripePaymentService,
            ILogger<CreateCheckoutSessionCommandHandler> logger)
        {
            _stripePaymentService = stripePaymentService;
            _logger = logger;
        }

        public async Task<CreateCheckoutSessionResponse> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating checkout session for tenant {TenantId}, plan {PlanId}", 
                    request.TenantId, request.PlanId);

                var checkoutRequest = new CheckoutSessionRequest
                {
                    TenantId = request.TenantId,
                    PlanId = request.PlanId,
                    CustomerEmail = request.CustomerEmail,
                    Currency = request.Currency
                };

                var checkoutSession = await _stripePaymentService.CreateCheckoutSessionAsync(checkoutRequest);

                _logger.LogInformation("Checkout session created successfully: {SessionId}", checkoutSession.SessionId);

                return new CreateCheckoutSessionResponse
                {
                    CheckoutUrl = checkoutSession.CheckoutUrl,
                    SessionId = checkoutSession.SessionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create checkout session for tenant {TenantId}, plan {PlanId}", 
                    request.TenantId, request.PlanId);
                throw;
            }
        }
    }
}

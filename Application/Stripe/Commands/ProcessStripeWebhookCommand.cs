using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Stripe.Commands
{
    public class ProcessStripeWebhookCommand
    {
        public string JsonPayload { get; set; } = string.Empty;
        public string StripeSignature { get; set; } = string.Empty;
    }

    public class ProcessStripeWebhookCommandHandler
    {
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<ProcessStripeWebhookCommandHandler> _logger;

        public ProcessStripeWebhookCommandHandler(
            IStripePaymentService stripePaymentService,
            ILogger<ProcessStripeWebhookCommandHandler> logger)
        {
            _stripePaymentService = stripePaymentService;
            _logger = logger;
        }

        public async Task Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing Stripe webhook");

                await _stripePaymentService.ProcessWebhookAsync(request.JsonPayload, request.StripeSignature);

                _logger.LogInformation("Stripe webhook processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Stripe webhook");
                throw;
            }
        }
    }
}

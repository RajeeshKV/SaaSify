using Application.Common.Interfaces;
using Application.Stripe.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Stripe.Commands
{
    public class ProcessStripeWebhookCommandHandler : IRequestHandler<ProcessStripeWebhookCommand, Unit>
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

        public async Task<Unit> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing Stripe webhook");

                await _stripePaymentService.ProcessWebhookAsync(request.JsonPayload, request.StripeSignature);

                _logger.LogInformation("Stripe webhook processed successfully");

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Stripe webhook");
                throw;
            }
        }
    }
}

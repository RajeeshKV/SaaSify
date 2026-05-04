using MediatR;

namespace Application.Stripe.Commands
{
    public record ProcessStripeWebhookCommand : IRequest<Unit>
    {
        public string JsonPayload { get; set; } = string.Empty;
        public string StripeSignature { get; set; } = string.Empty;
    }
}

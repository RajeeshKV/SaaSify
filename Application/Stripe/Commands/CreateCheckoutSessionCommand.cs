using Application.Common.Interfaces;
using MediatR;

namespace Application.Stripe.Commands
{
    public record CreateCheckoutSessionCommand : IRequest<CreateCheckoutSessionResponse>
    {
        public int TenantId { get; set; }
        public string PlanId { get; set; }
        public string? CustomerEmail { get; set; }
        public string Currency { get; set; } = "usd";
    }

    public record CreateCheckoutSessionResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }
}

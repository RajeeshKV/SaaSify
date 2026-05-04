using MediatR;

namespace Application.Stripe.Queries
{
    public record HandleStripeSuccessQuery : IRequest<HandleStripeSuccessResponse>
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public record HandleStripeSuccessResponse
    {
        public string RedirectUrl { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}

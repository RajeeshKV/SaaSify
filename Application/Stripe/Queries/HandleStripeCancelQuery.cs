using MediatR;

namespace Application.Stripe.Queries
{
    public record HandleStripeCancelQuery : IRequest<HandleStripeCancelResponse>
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public record HandleStripeCancelResponse
    {
        public string RedirectUrl { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}

using Application.Common.Interfaces;
using Application.Stripe.Queries;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Stripe.Queries
{
    public class HandleStripeSuccessQueryHandler : IRequestHandler<HandleStripeSuccessQuery, HandleStripeSuccessResponse>
    {
        private readonly ILogger<HandleStripeSuccessQueryHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IStripePaymentService _stripePaymentService;

        public HandleStripeSuccessQueryHandler(
            ILogger<HandleStripeSuccessQueryHandler> logger,
            IConfiguration configuration,
            IStripePaymentService stripePaymentService)
        {
            _logger = logger;
            _configuration = configuration;
            _stripePaymentService = stripePaymentService;
        }

        public async Task<HandleStripeSuccessResponse> Handle(HandleStripeSuccessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Stripe success callback received for session: {SessionId}", request.SessionId);
                
                // Delegate to the service layer to handle Stripe logic
                var isValid = await _stripePaymentService.ValidateSessionAsync(request.SessionId);

                if (!isValid)
                {
                    _logger.LogError("Invalid Stripe session: {SessionId}", request.SessionId);
                    return new HandleStripeSuccessResponse
                    {
                        Success = false,
                        Error = "Invalid session"
                    };
                }

                _logger.LogInformation("Stripe session validated: {SessionId}", request.SessionId);

                // Redirect to frontend success page
                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://saasify.rajeesh.online";
                var redirectUrl = $"{frontendUrl}/billing/success?session_id={request.SessionId}&success=true";
                
                _logger.LogInformation("Redirecting to frontend: {RedirectUrl}", redirectUrl);

                return new HandleStripeSuccessResponse
                {
                    Success = true,
                    RedirectUrl = redirectUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe success callback for session: {SessionId}", request.SessionId);
                
                // Redirect to frontend with error
                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://saasify.rajeesh.online";
                var redirectUrl = $"{frontendUrl}/billing/error?session_id={request.SessionId}&error={Uri.EscapeDataString(ex.Message)}";

                return new HandleStripeSuccessResponse
                {
                    Success = false,
                    Error = ex.Message,
                    RedirectUrl = redirectUrl
                };
            }
        }
    }
}

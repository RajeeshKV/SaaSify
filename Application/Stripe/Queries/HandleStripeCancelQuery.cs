using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Stripe.Queries
{
    public class HandleStripeCancelQuery
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class HandleStripeCancelResponse
    {
        public string RedirectUrl { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class HandleStripeCancelQueryHandler
    {
        private readonly ILogger<HandleStripeCancelQueryHandler> _logger;
        private readonly IConfiguration _configuration;

        public HandleStripeCancelQueryHandler(
            ILogger<HandleStripeCancelQueryHandler> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<HandleStripeCancelResponse> Handle(HandleStripeCancelQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Stripe cancel callback received");
                
                // Redirect to frontend cancel page
                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://saasify.rajeesh.online";
                var redirectUrl = $"{frontendUrl}/billing/cancel?cancelled=true";
                
                _logger.LogInformation("Redirecting to frontend cancel page: {RedirectUrl}", redirectUrl);

                return new HandleStripeCancelResponse
                {
                    Success = true,
                    RedirectUrl = redirectUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe cancel callback");
                
                return new HandleStripeCancelResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}

using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Application.Orders.Queries
{
    public class HandleOrderSuccessQuery
    {
        public string SessionId { get; set; }
    }

    public class HandleOrderSuccessQueryHandler
    {
        private readonly IConfiguration _configuration;

        public HandleOrderSuccessQueryHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<HandleStripeSuccessQueryResult> Handle(HandleOrderSuccessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // For order success, redirect to frontend order success page
                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://saasify.rajeesh.online";
                var redirectUrl = $"{frontendUrl}/order/success?session_id={request.SessionId}&success=true";

                return new HandleStripeSuccessQueryResult
                {
                    Success = true,
                    RedirectUrl = redirectUrl
                };
            }
            catch (Exception ex)
            {
                return new HandleStripeSuccessQueryResult
                {
                    Success = false,
                    Error = $"Failed to handle order success: {ex.Message}"
                };
            }
        }
    }
}

using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Application.Orders.Queries
{
    public class HandleOrderCancelQuery
    {
        public string SessionId { get; set; }
    }

    public class HandleOrderCancelQueryHandler
    {
        private readonly IConfiguration _configuration;

        public HandleOrderCancelQueryHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<HandleStripeCancelQueryResult> Handle(HandleOrderCancelQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // For order cancel, redirect to frontend order cancel page
                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://saasify.rajeesh.online";
                var redirectUrl = $"{frontendUrl}/order/cancel?session_id={request.SessionId}&cancelled=true";

                return new HandleStripeCancelQueryResult
                {
                    Success = true,
                    RedirectUrl = redirectUrl
                };
            }
            catch (Exception ex)
            {
                return new HandleStripeCancelQueryResult
                {
                    Success = false,
                    Error = $"Failed to handle order cancel: {ex.Message}"
                };
            }
        }
    }
}

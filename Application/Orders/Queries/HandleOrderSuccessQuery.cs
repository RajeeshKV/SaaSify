using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Stripe;

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
                // Retrieve order details from Stripe session
                var sessionService = new Stripe.Checkout.SessionService();
                var session = await sessionService.GetAsync(request.SessionId);

                if (session == null)
                {
                    return new HandleStripeSuccessQueryResult
                    {
                        Success = false,
                        Error = "Invalid session"
                    };
                }

                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://saasify.rajeesh.online";
                
                // Build redirect URL with order details
                var redirectParams = new List<string>
                {
                    $"session_id={request.SessionId}",
                    "success=true"
                };

                // Add order details if available from session metadata
                if (session.Metadata != null && session.Metadata.ContainsKey("order_id"))
                {
                    redirectParams.Add($"order_id={session.Metadata["order_id"]}");
                }

                // Add payment details
                redirectParams.Add($"amount={session.AmountTotal / 100.0m}"); // Convert from cents
                redirectParams.Add($"currency={session.Currency.ToUpper()}");
                redirectParams.Add($"payment_status={session.PaymentStatus}");
                redirectParams.Add($"date={Uri.EscapeDataString(session.Created.ToString("yyyy-MM-dd"))}");

                var redirectUrl = $"{frontendUrl}/order/success?{string.Join("&", redirectParams)}";

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

using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Stripe;
using Stripe.Checkout;

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
                var sessionService = new SessionService();
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
                
                // Check if order was created based on session metadata and payment status
                // The webhook would have created the order if payment was successful
                var orderWasCreated = false;
                var actualOrderId = "";
                
                // Check session metadata for order creation indicators
                if (session.Metadata != null)
                {
                    // If session has order_id metadata, it means order was created successfully
                    if (session.Metadata.ContainsKey("order_id") && !string.IsNullOrEmpty(session.Metadata["order_id"]))
                    {
                        orderWasCreated = true;
                        actualOrderId = session.Metadata["order_id"];
                    }
                    // Check payment status - if paid but no order_id, likely failed
                    else if (session.PaymentStatus == "paid")
                    {
                        orderWasCreated = false; // Payment successful but order creation failed
                    }
                }
                else
                {
                    // No metadata means we can't determine order status
                    orderWasCreated = false;
                }

                // Build redirect URL with appropriate status
                var redirectParams = new List<string>
                {
                    $"session_id={request.SessionId}",
                    orderWasCreated ? "success=true" : "success=false",
                    orderWasCreated ? "status=completed" : "status=failed"
                };

                // Add order details if available
                if (!string.IsNullOrEmpty(actualOrderId))
                {
                    redirectParams.Add($"order_id={actualOrderId}");
                }

                // Add payment details
                redirectParams.Add($"amount={session.AmountTotal / 100.0m}"); // Convert from cents
                redirectParams.Add($"currency={session.Currency.ToUpper()}");
                redirectParams.Add($"payment_status={(orderWasCreated ? "paid" : "refunded")}");
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

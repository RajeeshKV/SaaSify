using Application.Common.Interfaces;
using Application.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Infrastructure.Services
{
    public class UnifiedStripePaymentService : IStripePaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UnifiedStripePaymentService> _logger;
        private readonly ISubscriptionService _subscriptionService;

        public UnifiedStripePaymentService(
            IConfiguration configuration, 
            ILogger<UnifiedStripePaymentService> logger,
            ISubscriptionService subscriptionService)
        {
            _configuration = configuration;
            _logger = logger;
            _subscriptionService = subscriptionService;
            
            // Configure Stripe
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<Application.Common.Interfaces.PaymentIntentResponse> CreatePaymentIntentAsync(PaymentIntentRequest request)
        {
            try
            {
                // Add tenant and user metadata
                request.Metadata["tenant_id"] = request.TenantId.ToString();
                request.Metadata["user_id"] = request.UserId.ToString();
                request.Metadata["payment_type"] = request.PaymentType;

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100), // Convert to cents
                    Currency = request.Currency,
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = request.Metadata
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                _logger.LogInformation("Payment intent created: {PaymentIntentId} for tenant {TenantId}, amount {Amount}", 
                    paymentIntent.Id, request.TenantId, request.Amount);

                return new Application.Common.Interfaces.PaymentIntentResponse
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id,
                    Status = paymentIntent.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment intent for tenant {TenantId}", request.TenantId);
                throw new Exception($"Failed to create payment intent: {ex.Message}");
            }
        }

        public async Task<Application.Common.Interfaces.CheckoutSessionResponse> CreateCheckoutSessionAsync(CheckoutSessionRequest request)
        {
            try
            {
                var plan = GetPlanById(request.PlanId);
                if (plan == null)
                {
                    throw new Exception($"Plan '{request.PlanId}' not found. Available plans: Free, Professional, Enterprise");
                }

                // Add tenant metadata
                request.Metadata["tenant_id"] = request.TenantId.ToString();
                request.Metadata["plan_id"] = request.PlanId;
                request.Metadata["payment_type"] = "subscription";

                var options = new SessionCreateOptions
                {
                    CustomerEmail = request.CustomerEmail,
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(plan.MonthlyPrice * 100), // Convert to cents
                                Currency = request.Currency,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"{plan.Name} Plan",
                                    Description = plan.Description
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = $"{GetBaseUrl()}/api/stripe/success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{GetBaseUrl()}/api/stripe/cancel",
                    Metadata = request.Metadata
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                _logger.LogInformation("Checkout session created: {SessionId} for tenant {TenantId}, plan {PlanId}", 
                    session.Id, request.TenantId, request.PlanId);

                return new Application.Common.Interfaces.CheckoutSessionResponse
                {
                    SessionId = session.Id,
                    CheckoutUrl = session.Url,
                    Status = session.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create checkout session for tenant {TenantId}, plan {PlanId}", 
                    request.TenantId, request.PlanId);
                throw new Exception($"Failed to create checkout session: {ex.Message}");
            }
        }

        public async Task ProcessWebhookAsync(string jsonBody, string signature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(jsonBody, signature, webhookSecret);

                _logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

                switch (stripeEvent.Type)
                {
                    case Events.CheckoutSessionCompleted:
                        await HandleCheckoutSessionCompleted(stripeEvent.Data.Object as Session);
                        break;
                    case Events.PaymentIntentSucceeded:
                        await HandlePaymentIntentSucceeded(stripeEvent.Data.Object as PaymentIntent);
                        break;
                    case Events.PaymentIntentPaymentFailed:
                        await HandlePaymentIntentFailed(stripeEvent.Data.Object as PaymentIntent);
                        break;
                    case Events.InvoicePaymentSucceeded:
                        await HandleInvoicePaymentSucceeded(stripeEvent.Data.Object as Invoice);
                        break;
                    case Events.CustomerSubscriptionDeleted:
                        await HandleSubscriptionDeleted(stripeEvent.Data.Object as Stripe.Subscription);
                        break;
                    default:
                        _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Stripe webhook");
                throw;
            }
        }

        public async Task<bool> ValidateSessionAsync(string sessionId)
        {
            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(sessionId);
                return session != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate Stripe session: {SessionId}", sessionId);
                return false;
            }
        }

        private async Task HandleCheckoutSessionCompleted(Session session)
        {
            if (session.Metadata == null)
                return;

            var paymentType = session.Metadata.GetValueOrDefault("payment_type");
            
            if (paymentType == "subscription")
            {
                await HandleSubscriptionCheckoutCompleted(session);
            }
            else if (paymentType == "order")
            {
                await HandleOrderCheckoutCompleted(session);
            }
        }

        private async Task HandleSubscriptionCheckoutCompleted(Session session)
        {
            try
            {
                var tenantId = int.Parse(session.Metadata["tenant_id"]);
                var planId = session.Metadata["plan_id"];

                var plan = GetPlanById(planId);
                if (plan == null)
                {
                    _logger.LogError("Plan not found: {PlanId} for tenant {TenantId}", planId, tenantId);
                    return;
                }

                _logger.LogInformation("Processing subscription completion for tenant {TenantId}, plan {PlanId}", tenantId, planId);
                
                // Create subscription using the subscription service
                var subscription = await _subscriptionService.CreateSubscriptionAsync(tenantId, planId, plan.MonthlyPrice);
                
                _logger.LogInformation("Subscription activated successfully for tenant {TenantId}, plan {PlanId}, subscription ID: {SubscriptionId}", 
                    tenantId, planId, subscription.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate subscription for session {SessionId}", session.Id);
                throw;
            }
        }

        private async Task HandleOrderCheckoutCompleted(Session session)
        {
            var tenantId = int.Parse(session.Metadata["tenant_id"]);
            var orderId = session.Metadata.GetValueOrDefault("order_id");
            
            _logger.LogInformation("Order payment completed for tenant {TenantId}, order {OrderId}", tenantId, orderId);
            
            // TODO: Update order status in database
            // This would require injecting ApplicationDbContext or using a service
        }

        private async Task HandlePaymentIntentSucceeded(PaymentIntent paymentIntent)
        {
            var paymentType = paymentIntent.Metadata.GetValueOrDefault("payment_type");
            var tenantIdStr = paymentIntent.Metadata.GetValueOrDefault("tenant_id");
            
            _logger.LogInformation("Payment intent succeeded for tenant {TenantId}, type {PaymentType}", tenantIdStr, paymentType);
        }

        private async Task HandlePaymentIntentFailed(PaymentIntent paymentIntent)
        {
            var paymentType = paymentIntent.Metadata.GetValueOrDefault("payment_type");
            var tenantId = paymentIntent.Metadata.GetValueOrDefault("tenant_id");
            
            _logger.LogWarning("Payment intent failed for tenant {TenantId}, type {PaymentType}", tenantId, paymentType);
            
            // TODO: Handle payment intent failure
        }

        private async Task HandleInvoicePaymentSucceeded(Invoice invoice)
        {
            // Handle recurring subscription payments
            _logger.LogInformation("Invoice payment succeeded: {InvoiceId}", invoice.Id);
        }

        private async Task HandleSubscriptionDeleted(Stripe.Subscription subscription)
        {
            // Handle subscription cancellation
            _logger.LogInformation("Subscription deleted: {SubscriptionId}", subscription.Id);
        }

        private PlanConfiguration GetPlanById(string planId)
        {
            var plans = _configuration.GetSection("Subscription:Plans").GetChildren();
            foreach (var planSection in plans)
            {
                var planName = planSection["Name"];
                if (planName == planId)
                {
                    var plan = new PlanConfiguration
                    {
                        Name = planName ?? "",
                        Description = planSection["Description"] ?? "",
                        MonthlyPrice = decimal.Parse(planSection["MonthlyPrice"] ?? "0"),
                        RateLimitPerMinute = int.Parse(planSection["RateLimitPerMinute"] ?? "100"),
                        MaxUsers = int.Parse(planSection["MaxUsers"] ?? "3"),
                        Features = planSection.GetSection("Features").GetChildren().Select(f => f.Value ?? "").ToList()
                    };
                    return plan;
                }
            }
            return null;
        }

        private string GetBaseUrl()
        {
            return _configuration["Stripe:BaseUrl"] ?? "https://saasifyapi.rajeesh.online";
        }
    }
}

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
        private readonly IOrderServiceClient _orderServiceClient;

        public UnifiedStripePaymentService(
            IConfiguration configuration, 
            ILogger<UnifiedStripePaymentService> logger,
            ISubscriptionService subscriptionService,
            IOrderServiceClient orderServiceClient)
        {
            _configuration = configuration;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _orderServiceClient = orderServiceClient;
            
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
                // Check if this is an order payment (PlanId = "order")
                if (request.PlanId == "order")
                {
                    // Handle order payment
                    return await CreateOrderCheckoutSessionAsync(request);
                }

                // Handle subscription payment
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
                    SuccessUrl = $"{GetBaseUrl()}/api/v1/stripe/success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{GetBaseUrl()}/api/v1/stripe/cancel",
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
                _logger.LogInformation("Webhook received. JsonBody length: {JsonBodyLength}, Signature: {Signature}", 
                    jsonBody?.Length ?? 0, signature?.Substring(0, Math.Min(20, signature?.Length ?? 0)) + "...");

                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                if (string.IsNullOrEmpty(webhookSecret))
                {
                    _logger.LogError("Stripe webhook secret is not configured");
                    throw new InvalidOperationException("Stripe webhook secret is not configured");
                }

                var stripeEvent = EventUtility.ConstructEvent(jsonBody, signature, webhookSecret, throwOnApiVersionMismatch: false);

                _logger.LogInformation("Processing Stripe webhook event: {EventType}, Event ID: {EventId}", stripeEvent.Type, stripeEvent.Id);

                switch (stripeEvent.Type)
                {
                    case Events.CheckoutSessionCompleted:
                        _logger.LogInformation("Handling CheckoutSessionCompleted event");
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
            _logger.LogInformation("HandleCheckoutSessionCompleted called for session {SessionId}", session.Id);
            
            if (session.Metadata == null)
            {
                _logger.LogError("Session metadata is null for session {SessionId}", session.Id);
                return;
            }

            _logger.LogInformation("Session metadata for checkout completion: {Metadata}", 
                string.Join(", ", session.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}")));

            var paymentType = session.Metadata.GetValueOrDefault("payment_type");
            
            _logger.LogInformation("Payment type detected: {PaymentType} for session {SessionId}", paymentType, session.Id);
            
            if (paymentType == "subscription")
            {
                _logger.LogInformation("Processing subscription checkout completion for session {SessionId}", session.Id);
                await HandleSubscriptionCheckoutCompleted(session);
            }
            else if (paymentType == "order")
            {
                _logger.LogInformation("Processing order checkout completion for session {SessionId}", session.Id);
                await HandleOrderCheckoutCompleted(session);
            }
            else
            {
                _logger.LogWarning("Unknown payment type '{PaymentType}' for session {SessionId}", paymentType, session.Id);
            }
        }

        private async Task HandleSubscriptionCheckoutCompleted(Session session)
        {
            try
            {
                _logger.LogInformation("HandleSubscriptionCheckoutCompleted called for session {SessionId}", session.Id);
                
                if (session.Metadata == null)
                {
                    _logger.LogError("Session metadata is null for session {SessionId}", session.Id);
                    return;
                }

                _logger.LogInformation("Session metadata: {Metadata}", string.Join(", ", session.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                if (!session.Metadata.ContainsKey("tenant_id"))
                {
                    _logger.LogError("tenant_id not found in metadata for session {SessionId}", session.Id);
                    return;
                }

                if (!session.Metadata.ContainsKey("plan_id"))
                {
                    _logger.LogError("plan_id not found in metadata for session {SessionId}", session.Id);
                    return;
                }

                var tenantId = int.Parse(session.Metadata["tenant_id"]);
                var planId = session.Metadata["plan_id"];

                _logger.LogInformation("Extracted tenantId: {TenantId}, planId: {PlanId} from session {SessionId}", tenantId, planId, session.Id);

                var plan = GetPlanById(planId);
                if (plan == null)
                {
                    _logger.LogError("Plan not found: {PlanId} for tenant {TenantId}", planId, tenantId);
                    return;
                }

                _logger.LogInformation("Found plan: {PlanName} with price {Price} for tenant {TenantId}", plan.Name, plan.MonthlyPrice, tenantId);
                
                // Create subscription using the subscription service
                _logger.LogInformation("Calling CreateSubscriptionAsync for tenant {TenantId}, plan {PlanId}, amount {Amount}", tenantId, planId, plan.MonthlyPrice);
                
                var subscription = await _subscriptionService.CreateSubscriptionAsync(tenantId, planId, plan.MonthlyPrice);
                
                _logger.LogInformation("Subscription activated successfully for tenant {TenantId}, plan {PlanId}, subscription ID: {SubscriptionId}, StartDate: {StartDate}, EndDate: {EndDate}", 
                    tenantId, planId, subscription.Id, subscription.StartDate, subscription.EndDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate subscription for session {SessionId}. Error details: {ErrorDetails}", session.Id, ex.ToString());
                throw;
            }
        }

        private async Task HandleOrderCheckoutCompleted(Session session)
        {
            var tenantId = int.Parse(session.Metadata["tenant_id"]);
            var orderId = session.Metadata.GetValueOrDefault("order_id");
            
            _logger.LogInformation("Order payment completed for tenant {TenantId}, order {OrderId}", tenantId, orderId);
            
            try
            {
                // Create order in OrderService after successful payment
                var accessToken = await GetAccessTokenForTenant(tenantId);
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Failed to get access token for tenant {TenantId}", tenantId);
                    return;
                }

                var orderRequest = new
                {
                    Amount = (decimal)session.AmountTotal / 100, // Convert from cents
                    Currency = session.Currency.ToUpper(),
                    Description = $"Payment for order {orderId}",
                    CustomerEmail = session.CustomerDetails?.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        {"stripe_session_id", session.Id},
                        {"payment_status", session.PaymentStatus},
                        {"paid_at", DateTime.UtcNow.ToString("O")}
                    }
                };

                // This would require OrderService to have a create order endpoint
                // For now, we'll log the successful payment
                _logger.LogInformation("Order payment processed successfully: TenantId={TenantId}, Amount={Amount}, SessionId={SessionId}", 
                    tenantId, session.AmountTotal / 100, session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order in OrderService for tenant {TenantId}", tenantId);
                // Payment was successful but order creation failed - this needs manual intervention
                // Consider implementing refund logic here
            }
        }

        private async Task<string> GetAccessTokenForTenant(int tenantId)
        {
            // TODO: Implement proper token generation for OrderService calls
            // For now, return empty string to indicate no token available
            // In a real implementation, you would:
            // 1. Generate a service-to-service token
            // 2. Include tenant context
            // 3. Return the token for OrderService API calls
            
            _logger.LogWarning("GetAccessTokenForTenant not implemented for tenant {TenantId}", tenantId);
            return string.Empty;
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

        private async Task<Application.Common.Interfaces.CheckoutSessionResponse> CreateOrderCheckoutSessionAsync(CheckoutSessionRequest request)
        {
            try
            {
                // Add order metadata
                request.Metadata["tenant_id"] = request.TenantId.ToString();
                request.Metadata["payment_type"] = "order";
                if (request.Metadata.ContainsKey("order_id"))
                {
                    request.Metadata["order_id"] = request.Metadata["order_id"];
                }

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
                                UnitAmount = (long)(request.Amount * 100), // Convert to cents
                                Currency = request.Currency,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Order Payment",
                                    Description = "Payment for order"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment", // One-time payment
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = request.Metadata
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                _logger.LogInformation("Order checkout session created: {SessionId} for tenant {TenantId}, amount {Amount}", 
                    session.Id, request.TenantId, request.Amount);

                return new Application.Common.Interfaces.CheckoutSessionResponse
                {
                    SessionId = session.Id,
                    CheckoutUrl = session.Url,
                    Status = session.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order checkout session for tenant {TenantId}", request.TenantId);
                throw new Exception($"Failed to create order checkout session: {ex.Message}");
            }
        }

        private string GetBaseUrl()
        {
            return _configuration["Stripe:BaseUrl"] ?? "https://saasifyapi.rajeesh.online";
        }
    }
}

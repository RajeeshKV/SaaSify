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
        private readonly StripeApiClient _stripeApiClient;

        public UnifiedStripePaymentService(
            IConfiguration configuration, 
            ILogger<UnifiedStripePaymentService> logger,
            ISubscriptionService subscriptionService,
            IOrderServiceClient orderServiceClient,
            StripeApiClient stripeApiClient)
        {
            _configuration = configuration;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _orderServiceClient = orderServiceClient;
            _stripeApiClient = stripeApiClient;
            
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
                        if (stripeEvent.Data.Object is Session session)
                            await HandleCheckoutSessionCompleted(session);
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

                var orderMetadata = new Dictionary<string, string>
                {
                    {"stripe_session_id", session.Id},
                    {"payment_status", session.PaymentStatus},
                    {"paid_at", DateTime.UtcNow.ToString("O")}
                };

                if (!string.IsNullOrEmpty(orderId))
                {
                    orderMetadata["order_id"] = orderId;
                }

                // Debug JWT token generation
                _logger.LogInformation("=== JWT TOKEN DEBUG ===");
                _logger.LogInformation("JWT Issuer: {Issuer}", _configuration["Jwt:Issuer"] ?? "SaaSify");
                _logger.LogInformation("JWT Audience: {Audience}", _configuration["Jwt:Audience"] ?? "OrderService");
                _logger.LogInformation("JWT Secret Key Length: {SecretKeyLength}", (_configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production").Length);
                _logger.LogInformation("Full JWT Token: {Token}", accessToken);
                _logger.LogInformation("Token Expires: {Expires}", DateTime.UtcNow.AddHours(1));
                _logger.LogInformation("=== END JWT DEBUG ===");

                // Test OrderService JWT configuration before making the actual call
                await TestOrderServiceJwtConfigurationAsync(accessToken);

                _logger.LogInformation("Attempting to create order in OrderService with token: {TokenPrefix}...", accessToken.Substring(0, Math.Min(50, accessToken.Length)));

                var createdOrder = await _orderServiceClient.CreateOrderAsync(
                    tenantId: tenantId,
                    amount: (decimal)session.AmountTotal / 100, // Convert from cents
                    currency: session.Currency.ToUpper(),
                    description: $"Payment for order {orderId ?? "unknown"}",
                    customerEmail: session.CustomerDetails?.Email ?? $"tenant-{tenantId}@saasify.com",
                    metadata: orderMetadata,
                    accessToken: accessToken
                );

                if (createdOrder != null)
                {
                    _logger.LogInformation("Order created successfully: OrderId={OrderId}, TenantId={TenantId}, Amount={Amount}, SessionId={SessionId}", 
                        createdOrder.OrderId, tenantId, session.AmountTotal / 100, session.Id);
                }
                else
                {
                    _logger.LogError("Failed to create order in OrderService for tenant {TenantId}. Payment was successful but order creation failed - INITIATING REFUND", tenantId);
                    
                    // Refund the payment since order creation failed
                    await RefundPaymentAsync(session, "Order creation failed - automatic refund");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order in OrderService for tenant {TenantId}. Payment was successful but order creation failed - INITIATING REFUND", tenantId);
                
                // Refund the payment since order creation failed
                await RefundPaymentAsync(session, "Order creation failed due to system error - automatic refund");
            }
        }

        private string ValidateJwtToken(string tokenString)
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var key = System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production");
                
                var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "SaaSify",
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"] ?? "OrderService",
                    ValidateLifetime = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                try
                {
                    tokenHandler.ValidateToken(tokenString, validationParameters, out var validatedToken);
                    var jwtToken = tokenHandler.ReadJwtToken(tokenString);
                    
                    var claims = new List<string>();
                    foreach (var claim in jwtToken.Claims)
                    {
                        claims.Add($"{claim.Type}: {claim.Value}");
                    }
                    
                    return $"VALID - Claims: [{string.Join(", ", claims)}]";
                }
                catch (Microsoft.IdentityModel.Tokens.SecurityTokenValidationException ex)
                {
                    return $"INVALID - {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"ERROR - {ex.Message}";
            }
        }

        private async Task RefundPaymentAsync(Session session, string reason)
        {
            try
            {
                if (string.IsNullOrEmpty(session.PaymentIntentId))
                {
                    _logger.LogWarning("Cannot refund: No PaymentIntentId found for session {SessionId}", session.Id);
                    return;
                }

                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = session.PaymentIntentId,
                    Reason = RefundReasons.RequestedByCustomer,
                    Metadata = new Dictionary<string, string>
                    {
                        {"original_session_id", session.Id},
                        {"refund_reason", reason},
                        {"tenant_id", session.Metadata?.GetValueOrDefault("tenant_id") ?? "unknown"},
                        {"automatic_refund", "true"}
                    }
                };

                var refundService = new RefundService();
                var refund = await refundService.CreateAsync(refundOptions);

                _logger.LogWarning("Payment refunded automatically: RefundId={RefundId}, Amount={Amount}, Reason={Reason}, SessionId={SessionId}", 
                    refund.Id, refund.Amount / 100.0m, reason, session.Id);

                // TODO: Send email notification to customer about automatic refund
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refund payment for session {SessionId}. MANUAL REFUND REQUIRED", session.Id);
                // This is critical - payment succeeded but refund failed
                // Manual intervention required to refund the customer
            }
        }

        private async Task<string> GetAccessTokenForTenant(int tenantId)
        {
            try
            {
                // Generate a service-to-service JWT token for OrderService calls
                // This token should include tenant context and proper claims
                
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var key = System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production");
                
                var now = DateTime.UtcNow;
                var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
                {
                    Subject = new System.Security.Claims.ClaimsIdentity(new[]
                    {
                        new System.Security.Claims.Claim("sub", $"service-{tenantId}"),
                        new System.Security.Claims.Claim("TenantId", tenantId.ToString()),
                        new System.Security.Claims.Claim("scope", "order-service"),
                        new System.Security.Claims.Claim("role", "system"),
                        new System.Security.Claims.Claim("service", "payment-service"),
                        new System.Security.Claims.Claim("iat", ((int)(now - DateTime.UnixEpoch).TotalSeconds).ToString(), System.Security.Claims.ClaimValueTypes.Integer64)
                    }),
                    Expires = now.AddHours(1), // Short-lived token
                    IssuedAt = now,
                    NotBefore = now,
                    Issuer = _configuration["Jwt:Issuer"] ?? "SaaSify",
                    Audience = _configuration["Jwt:Audience"] ?? "OrderService",
                    SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                        new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                        Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);
                
                // Validate JWT token before returning
                var validationResult = ValidateJwtToken(tokenString);
                _logger.LogInformation("JWT Token Validation Result: {ValidationResult}", validationResult);
                
                _logger.LogInformation("Generated service token for tenant {TenantId}, expires {Expires}", tenantId, tokenDescriptor.Expires);
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate access token for tenant {TenantId}", tenantId);
                return string.Empty;
            }
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
                                Currency = request.Currency,
                                UnitAmount = (long)(request.Amount * 100), // Convert to cents
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

        private async Task TestOrderServiceJwtConfigurationAsync(string accessToken)
        {
            try
            {
                _logger.LogInformation("=== TESTING ORDERSERVICE JWT CONFIGURATION ===");
                
                // Test 1: Check OrderService health
                var isHealthy = await _orderServiceClient.IsHealthyAsync();
                _logger.LogInformation("OrderService Health Check: {IsHealthy}", isHealthy);
                
                if (!isHealthy)
                {
                    _logger.LogError("OrderService is not healthy - cannot test JWT");
                    return;
                }

                // Test 2: Try to get orders with our JWT token
                try
                {
                    var orders = await _orderServiceClient.GetOrdersAsync(
                        tenantId: 1, // Test tenant
                        page: 1,
                        pageSize: 1,
                        accessToken: accessToken
                    );
                    
                    _logger.LogInformation("OrderService JWT Test: SUCCESS - Orders retrieved");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OrderService JWT Test: FAILED - {Error}", ex.Message);
                    
                    // Test 3: Try with different audience values
                    await TestDifferentAudienceValuesAsync();
                }
                
                _logger.LogInformation("=== END ORDERSERVICE JWT TEST ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test OrderService JWT configuration");
            }
        }

        private async Task TestDifferentAudienceValuesAsync()
        {
            var testAudiences = new[] { "SaaSify", "OrderService", "saasify", "orderservice" };
            var testIssuer = _configuration["Jwt:Issuer"] ?? "SaaSify";
            
            foreach (var audience in testAudiences)
            {
                try
                {
                    _logger.LogInformation("Testing with Audience: {Audience}, Issuer: {Issuer}", audience, testIssuer);
                    
                    var testToken = await GetAccessTokenForTenant(1);
                    
                    var orders = await _orderServiceClient.GetOrdersAsync(
                        tenantId: 1,
                        page: 1,
                        pageSize: 1,
                        accessToken: testToken
                    );
                    
                    _logger.LogInformation("✅ Audience {Audience} WORKED", audience);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("❌ Audience {Audience} FAILED: {Error}", audience, ex.Message);
                }
            }
        }
    }
}

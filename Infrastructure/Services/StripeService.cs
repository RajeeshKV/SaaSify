using Application.Common.Interfaces;
using Application.Common.Configuration;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Infrastructure.Services
{
    public class StripeService : IStripeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public StripeService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            
            // Configure Stripe
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<string> CreateCheckoutSessionAsync(int tenantId, string planId)
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
                throw new Exception("Tenant not found");

            var plan = GetPlanById(planId);
            if (plan == null)
                throw new Exception("Plan not found");

            var options = new SessionCreateOptions
            {
                CustomerEmail = $"tenant-{tenantId}@saasify.com",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(plan.MonthlyPrice * 100), // Convert to cents
                            Currency = "usd",
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
                SuccessUrl = $"{GetBaseUrl()}/api/v1/subscription/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{GetBaseUrl()}/api/v1/subscription/cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "tenant_id", tenantId.ToString() },
                    { "plan_id", planId }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session.Url;
        }

        public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(decimal amount, string currency = "usd", Dictionary<string, string> metadata = null)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency,
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = metadata ?? new Dictionary<string, string>(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return new PaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }

        public async Task ProcessWebhookAsync(string jsonBody, string signature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(jsonBody, signature, webhookSecret);

                switch (stripeEvent.Type)
                {
                    case Events.CheckoutSessionCompleted:
                        if (stripeEvent.Data.Object is Session session)
                            await HandleCheckoutSessionCompleted(session);
                        break;
                    case Events.InvoicePaymentSucceeded:
                        if (stripeEvent.Data.Object is Invoice invoice)
                            await HandleInvoicePaymentSucceeded(invoice);
                        break;
                    case Events.CustomerSubscriptionDeleted:
                        if (stripeEvent.Data.Object is Stripe.Subscription subscription)
                            await HandleSubscriptionDeleted(subscription);
                        break;
                }
            }
            catch (StripeException ex)
            {
                throw new Exception($"Stripe webhook processing failed: {ex.Message}");
            }
        }

        private async Task HandleCheckoutSessionCompleted(Session session)
        {
            if (session.Metadata == null)
                return;

            var tenantId = int.Parse(session.Metadata["tenant_id"]);
            var planId = session.Metadata["plan_id"];

            var plan = GetPlanById(planId);
            if (plan == null)
                return;

            // Update tenant subscription
            var subscription = new Domain.Entities.Subscription
            {
                TenantId = tenantId,
                Plan = plan.Name,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true,
                Amount = plan.MonthlyPrice,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);

            // Update tenant plan
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant != null)
            {
                tenant.Plan = plan.Name;
            }

            await _context.SaveChangesAsync();
        }

        private async Task HandleInvoicePaymentSucceeded(Invoice invoice)
        {
            // Handle recurring subscription payments
            // This would extend the subscription end date
            await Task.CompletedTask;
        }

        private async Task HandleSubscriptionDeleted(Stripe.Subscription subscription)
        {
            // Handle subscription cancellation
            // This would deactivate the subscription
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

    public interface IStripeService
    {
        Task<string> CreateCheckoutSessionAsync(int tenantId, string planId);
        Task<PaymentIntentResponse> CreatePaymentIntentAsync(decimal amount, string currency = "usd", Dictionary<string, string> metadata = null);
        Task ProcessWebhookAsync(string jsonBody, string signature);
    }

    public class PaymentIntentResponse
    {
        public string ClientSecret { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

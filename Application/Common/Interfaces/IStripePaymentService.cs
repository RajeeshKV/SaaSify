namespace Application.Common.Interfaces
{
    public interface IStripePaymentService
    {
        Task<PaymentIntentResponse> CreatePaymentIntentAsync(PaymentIntentRequest request);
        Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(CheckoutSessionRequest request);
        Task ProcessWebhookAsync(string jsonBody, string signature);
    }

    public abstract class PaymentRequestBase
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public Dictionary<string, string> Metadata { get; set; } = new();
        public string CustomerEmail { get; set; }
    }

    public class PaymentIntentRequest : PaymentRequestBase
    {
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string PaymentType { get; set; } // "order" or "subscription"
    }

    public class CheckoutSessionRequest : PaymentRequestBase
    {
        public int TenantId { get; set; }
        public string PlanId { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
    }

    public class PaymentIntentResponse
    {
        public string ClientSecret { get; set; }
        public string PaymentIntentId { get; set; }
        public string Status { get; set; }
    }

    public class CheckoutSessionResponse
    {
        public string SessionId { get; set; }
        public string CheckoutUrl { get; set; }
        public string Status { get; set; }
    }

    public enum PaymentType
    {
        Order,
        Subscription
    }
}

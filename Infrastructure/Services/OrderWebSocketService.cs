using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace Infrastructure.Services
{
    public class OrderWebSocketService
    {
        private readonly IHubContext<OrderNotificationHub> _hubContext;
        private readonly ILogger<OrderWebSocketService> _logger;

        public OrderWebSocketService(IHubContext<OrderNotificationHub> hubContext, 
            ILogger<OrderWebSocketService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyOrderStatusUpdate(int tenantId, int orderId, string status, string eventType)
        {
            var groupName = $"tenant_{tenantId}_order_{orderId}";
            var notification = new
            {
                orderId,
                status,
                eventType,
                timestamp = DateTime.UtcNow,
                paymentType = "order",
                data = new { message = $"Order status updated to {status}" }
            };

            await _hubContext.Clients.Group(groupName).SendAsync("OrderStatusUpdate", notification);
            
            _logger.LogInformation("Sent order status update to group {GroupName}: {Status}", 
                groupName, status);
        }

        public async Task NotifySubscriptionStatusUpdate(int tenantId, string planId, string status, string eventType)
        {
            var groupName = $"tenant_{tenantId}_subscription";
            var notification = new
            {
                planId,
                status,
                eventType,
                timestamp = DateTime.UtcNow,
                paymentType = "subscription",
                data = new { message = $"Subscription status updated to {status}" }
            };

            await _hubContext.Clients.Group(groupName).SendAsync("SubscriptionStatusUpdate", notification);
            
            _logger.LogInformation("Sent subscription status update to group {GroupName}: {Status}", 
                groupName, status);
        }

        public async Task NotifyOrderCreated(int tenantId, int orderId, decimal amount, string customerEmail)
        {
            var tenantGroup = $"tenant_{tenantId}";
            var notification = new
            {
                orderId,
                amount,
                customerEmail,
                status = "pending",
                eventType = "OrderCreated",
                timestamp = DateTime.UtcNow,
                data = new { message = $"New order #{orderId} created" }
            };

            await _hubContext.Clients.Group(tenantGroup).SendAsync("OrderCreated", notification);
            
            _logger.LogInformation("Sent order created notification to tenant group {TenantId}", tenantId);
        }

        public async Task NotifyPaymentCompleted(int tenantId, int orderId, string paymentIntentId)
        {
            var groupName = $"tenant_{tenantId}_order_{orderId}";
            var notification = new
            {
                orderId,
                paymentIntentId,
                status = "processing",
                eventType = "PaymentCompleted",
                timestamp = DateTime.UtcNow,
                data = new { message = "Payment completed, processing order" }
            };

            await _hubContext.Clients.Group(groupName).SendAsync("PaymentCompleted", notification);
            
            _logger.LogInformation("Sent payment completed notification for order {OrderId}", orderId);
        }

        public async Task NotifyOrderCompleted(int tenantId, int orderId)
        {
            var groupName = $"tenant_{tenantId}_order_{orderId}";
            var notification = new
            {
                orderId,
                status = "completed",
                eventType = "OrderCompleted",
                timestamp = DateTime.UtcNow,
                data = new { message = "Order completed successfully" }
            };

            await _hubContext.Clients.Group(groupName).SendAsync("OrderCompleted", notification);
            
            _logger.LogInformation("Sent order completed notification for order {OrderId}", orderId);
        }

        public async Task NotifyOrderFailed(int tenantId, int orderId, string errorMessage)
        {
            var groupName = $"tenant_{tenantId}_order_{orderId}";
            var notification = new
            {
                orderId,
                status = "failed",
                eventType = "OrderFailed",
                timestamp = DateTime.UtcNow,
                paymentType = "order",
                data = new { message = errorMessage }
            };

            await _hubContext.Clients.Group(groupName).SendAsync("OrderFailed", notification);
            
            _logger.LogInformation("Sent order failed notification for order {OrderId}", orderId);
        }

        public async Task NotifySubscriptionCreated(int tenantId, string planId, decimal amount, string customerEmail)
        {
            var tenantGroup = $"tenant_{tenantId}";
            var notification = new
            {
                planId,
                amount,
                customerEmail,
                status = "pending",
                eventType = "SubscriptionCreated",
                timestamp = DateTime.UtcNow,
                paymentType = "subscription",
                data = new { message = $"New subscription created for plan {planId}" }
            };

            await _hubContext.Clients.Group(tenantGroup).SendAsync("SubscriptionCreated", notification);
            
            _logger.LogInformation("Sent subscription created notification to tenant group {TenantId}", tenantId);
        }

        public async Task NotifySubscriptionPaymentCompleted(int tenantId, string planId, string sessionId)
        {
            var groupName = $"tenant_{tenantId}_subscription";
            var notification = new
            {
                planId,
                sessionId,
                status = "active",
                eventType = "SubscriptionPaymentCompleted",
                timestamp = DateTime.UtcNow,
                paymentType = "subscription",
                data = new { message = "Subscription payment completed, activating plan" }
            };

            await _hubContext.Clients.Group(groupName).SendAsync("SubscriptionPaymentCompleted", notification);
            
            _logger.LogInformation("Sent subscription payment completed notification for plan {PlanId}", planId);
        }

        public async Task NotifySubscriptionCompleted(int tenantId, string planId)
        {
            var groupName = $"tenant_{tenantId}_subscription";
            var notification = new
            {
                planId,
                status = "active",
                eventType = "SubscriptionCompleted",
                timestamp = DateTime.UtcNow,
                paymentType = "subscription",
                data = new { message = "Subscription activated successfully" }
            };

            await _hubContext.Clients.Group(groupName).SendAsync("SubscriptionCompleted", notification);
            
            _logger.LogInformation("Sent subscription completed notification for plan {PlanId}", planId);
        }

        public async Task NotifySubscriptionFailed(int tenantId, string planId, string errorMessage)
        {
            var groupName = $"tenant_{tenantId}_subscription";
            var notification = new
            {
                planId,
                status = "failed",
                eventType = "SubscriptionFailed",
                timestamp = DateTime.UtcNow,
                paymentType = "subscription",
                data = new { message = errorMessage }
            };

            await _hubContext.Clients.Group(groupName).SendAsync("SubscriptionFailed", notification);
            
            _logger.LogInformation("Sent subscription failed notification for plan {PlanId}", planId);
        }
    }
}

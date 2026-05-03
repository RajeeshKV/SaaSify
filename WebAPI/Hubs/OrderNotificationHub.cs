using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Infrastructure.Services;

namespace WebAPI.Hubs
{
    [Authorize]
    public class OrderNotificationHub : Hub
    {
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<OrderNotificationHub> _logger;

        public OrderNotificationHub(ITenantContext tenantContext, ILogger<OrderNotificationHub> logger)
        {
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task JoinOrderGroup(string orderId)
        {
            var tenantId = _tenantContext.TenantId;
            var groupName = $"tenant_{tenantId}_order_{orderId}";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} joined order group {GroupName}", 
                Context.UserIdentifier, groupName);
        }

        public async Task LeaveOrderGroup(string orderId)
        {
            var tenantId = _tenantContext.TenantId;
            var groupName = $"tenant_{tenantId}_order_{orderId}";
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} left order group {GroupName}", 
                Context.UserIdentifier, groupName);
        }

        public async Task JoinSubscriptionGroup()
        {
            var tenantId = _tenantContext.TenantId;
            var groupName = $"tenant_{tenantId}_subscription";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} joined subscription group {GroupName}", 
                Context.UserIdentifier, groupName);
        }

        public async Task LeaveSubscriptionGroup()
        {
            var tenantId = _tenantContext.TenantId;
            var groupName = $"tenant_{tenantId}_subscription";
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} left subscription group {GroupName}", 
                Context.UserIdentifier, groupName);
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = _tenantContext.TenantId;
            var userId = Context.UserIdentifier;
            
            // Join tenant-wide group for general notifications
            var tenantGroup = $"tenant_{tenantId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantGroup);
            
            _logger.LogInformation("User {UserId} connected to tenant {TenantId}", userId, tenantId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = Context.UserIdentifier;
            
            _logger.LogInformation("User {UserId} disconnected from tenant {TenantId}", userId, tenantId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}

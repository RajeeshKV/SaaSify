namespace Application.Common.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetCurrentSubscriptionAsync(int tenantId);
    Task<List<PlanDto>> GetAvailablePlansAsync();
    Task<SubscriptionDto> UpgradePlanAsync(int tenantId, string newPlan);
    Task<SubscriptionDto> CreateSubscriptionAsync(int tenantId, string plan, decimal amount);
    Task<bool> CancelSubscriptionAsync(int tenantId);
    Task<List<SubscriptionDto>> GetSubscriptionHistoryAsync(int tenantId);
}

public record SubscriptionDto(
    int Id,
    string Plan,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    decimal Amount,
    string Currency,
    DateTime CreatedAt
);

public record PlanDto(
    string Name,
    string Description,
    decimal MonthlyPrice,
    int RateLimitPerMinute,
    int MaxUsers,
    string[] Features
);

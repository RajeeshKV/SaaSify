using Application.Common.Interfaces;
using Application.Common.Configuration;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SubscriptionConfiguration _subscriptionConfig;

    public SubscriptionService(ApplicationDbContext context, IUnitOfWork unitOfWork, SubscriptionConfiguration subscriptionConfig)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _subscriptionConfig = subscriptionConfig;
    }

    public async Task<SubscriptionDto?> GetCurrentSubscriptionAsync(int tenantId)
    {
        var subscription = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null)
            return null;

        return new SubscriptionDto(
            subscription.Id,
            subscription.Plan,
            subscription.StartDate,
            subscription.EndDate,
            subscription.IsActive,
            subscription.Amount,
            subscription.Currency,
            subscription.CreatedAt
        );
    }

    public async Task<List<PlanDto>> GetAvailablePlansAsync()
    {
        return _subscriptionConfig.Plans.Select(plan => new PlanDto(
            plan.Name,
            plan.Description,
            plan.MonthlyPrice,
            plan.RateLimitPerMinute,
            plan.MaxUsers,
            plan.Features.ToArray()
        )).ToList();
    }

    public async Task<SubscriptionDto> UpgradePlanAsync(int tenantId, string newPlan)
    {
        var availablePlans = await GetAvailablePlansAsync();
        var plan = availablePlans.FirstOrDefault(p => p.Name == newPlan);
        
        if (plan == null)
            throw new ArgumentException($"Invalid plan: {newPlan}");

        // Deactivate current subscription if exists
        var currentSubscription = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .FirstOrDefaultAsync();

        if (currentSubscription != null)
        {
            currentSubscription.IsActive = false;
        }

        // Create new subscription
        var subscription = new Subscription
        {
            TenantId = tenantId,
            Plan = newPlan,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            Amount = plan.MonthlyPrice,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);

        // Update tenant plan
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant != null)
        {
            tenant.Plan = newPlan;
        }

        await _unitOfWork.SaveChangesAsync();

        return new SubscriptionDto(
            subscription.Id,
            subscription.Plan,
            subscription.StartDate,
            subscription.EndDate,
            subscription.IsActive,
            subscription.Amount,
            subscription.Currency,
            subscription.CreatedAt
        );
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(int tenantId, string plan, decimal amount)
    {
        var subscription = new Subscription
        {
            TenantId = tenantId,
            Plan = plan,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            Amount = amount,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);

        // Update tenant plan
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant != null)
        {
            tenant.Plan = plan;
        }

        await _unitOfWork.SaveChangesAsync();

        return new SubscriptionDto(
            subscription.Id,
            subscription.Plan,
            subscription.StartDate,
            subscription.EndDate,
            subscription.IsActive,
            subscription.Amount,
            subscription.Currency,
            subscription.CreatedAt
        );
    }

    public async Task<bool> CancelSubscriptionAsync(int tenantId)
    {
        var subscription = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .FirstOrDefaultAsync();

        if (subscription == null)
            return false;

        subscription.IsActive = false;
        subscription.EndDate = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<List<SubscriptionDto>> GetSubscriptionHistoryAsync(int tenantId)
    {
        var subscriptions = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return subscriptions.Select(s => new SubscriptionDto(
            s.Id,
            s.Plan,
            s.StartDate,
            s.EndDate,
            s.IsActive,
            s.Amount,
            s.Currency,
            s.CreatedAt
        )).ToList();
    }

    public async Task<int> GetRateLimitForTenantAsync(int tenantId)
    {
        var subscription = await GetCurrentSubscriptionAsync(tenantId);
        if (subscription == null)
            return 100; // Default rate limit

        return subscription.Plan switch
        {
            "Free" => 100,
            "Professional" => 1000,
            "Enterprise" => 5000,
            _ => 100
        };
    }
}

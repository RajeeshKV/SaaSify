using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionService(ApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
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
        return new List<PlanDto>
        {
            new PlanDto(
                "Free",
                "Perfect for small teams getting started",
                0,
                100,
                3,
                new[] { "Basic features", "3 users", "100 requests/minute" }
            ),
            new PlanDto(
                "Professional",
                "For growing teams that need more power",
                29.99m,
                1000,
                10,
                new[] { "All Free features", "10 users", "1000 requests/minute", "Priority support" }
            ),
            new PlanDto(
                "Enterprise",
                "For large organizations with advanced needs",
                99.99m,
                5000,
                50,
                new[] { "All Professional features", "50 users", "5000 requests/minute", "24/7 support", "Custom integrations" }
            )
        };
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

using Application.Common.Interfaces;
using Application.Common.Configuration;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

    private (DateTime StartDate, DateTime EndDate) CalculateSubscriptionDates(DateTime? currentStartDate = null)
    {
        // Use Indian time zone for calculation but store as UTC
        var indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var nowInIndia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indianTimeZone);
        
        // Calculate start date in IST (beginning of day)
        DateTime startDateInIndia;
        if (currentStartDate.HasValue)
        {
            startDateInIndia = TimeZoneInfo.ConvertTimeFromUtc(currentStartDate.Value, indianTimeZone);
        }
        else
        {
            startDateInIndia = new DateTime(nowInIndia.Year, nowInIndia.Month, nowInIndia.Day, 0, 0, 0, DateTimeKind.Unspecified);
        }
        
        // Calculate end date as exactly one month from start date in IST
        var endDateInIndia = startDateInIndia.AddMonths(1);
        
        // Convert both dates back to UTC for database storage
        var startDateUtc = TimeZoneInfo.ConvertTimeToUtc(startDateInIndia, indianTimeZone);
        var endDateUtc = TimeZoneInfo.ConvertTimeToUtc(endDateInIndia, indianTimeZone);
        
        return (startDateUtc, endDateUtc);
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

        // Deactivate ALL current subscriptions for this tenant
        var currentSubscriptions = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync();

        foreach (var sub in currentSubscriptions)
        {
            sub.IsActive = false;
            sub.UpdatedAt = DateTime.UtcNow;
        }

        // Calculate subscription dates with Indian time zone
        var (startDate, endDate) = CalculateSubscriptionDates(currentSubscriptions.FirstOrDefault()?.EndDate);
        
        // Create new subscription
        var subscription = new Subscription
        {
            TenantId = tenantId,
            Plan = newPlan,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            Amount = plan.MonthlyPrice,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);

        // Update tenant plan
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant != null)
        {
            tenant.Plan = newPlan;
            tenant.UpdatedAt = DateTime.UtcNow;
        }

        // Update tenant settings based on new plan
        await UpdateTenantSettingsForPlanAsync(tenantId, newPlan);

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

    public async Task<SubscriptionDto> DegradePlanAsync(int tenantId, string newPlan)
    {
        var availablePlans = await GetAvailablePlansAsync();
        var plan = availablePlans.FirstOrDefault(p => p.Name == newPlan);
        
        if (plan == null)
            throw new ArgumentException($"Invalid plan: {newPlan}");

        // Deactivate ALL current subscriptions for this tenant
        var currentSubscriptions = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync();

        foreach (var sub in currentSubscriptions)
        {
            sub.IsActive = false;
            sub.UpdatedAt = DateTime.UtcNow;
        }

        // Calculate subscription dates with Indian time zone
        var (startDate, endDate) = CalculateSubscriptionDates(currentSubscriptions.FirstOrDefault()?.EndDate);
        
        // Create new subscription without payment (degrade)
        var subscription = new Subscription
        {
            TenantId = tenantId,
            Plan = newPlan,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            Amount = 0, // No charge for downgrade
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);

        // Update tenant plan
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant != null)
        {
            tenant.Plan = newPlan;
            tenant.UpdatedAt = DateTime.UtcNow;
        }

        // Update tenant settings based on new plan
        await UpdateTenantSettingsForPlanAsync(tenantId, newPlan);

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

    private async Task UpdateTenantSettingsForPlanAsync(int tenantId, string plan)
    {
        var existingSettings = await _context.TenantSettings
            .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

        if (existingSettings == null)
        {
            existingSettings = new TenantSettings
            {
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };
            _context.TenantSettings.Add(existingSettings);
        }

        // Update settings based on plan
        switch (plan.ToLower())
        {
            case "free":
                existingSettings.MaxProjects = 5;
                existingSettings.MaxUsers = 3;
                existingSettings.EnableAdvancedFeatures = false;
                existingSettings.EnableApiAccess = false;
                existingSettings.EnableExport = false;
                existingSettings.EnableIntegrations = false;
                existingSettings.MaxStorageMB = 100;
                existingSettings.MaxApiCallsPerDay = 100;
                break;
                
            case "professional":
                existingSettings.MaxProjects = 50;
                existingSettings.MaxUsers = 10;
                existingSettings.EnableAdvancedFeatures = true;
                existingSettings.EnableApiAccess = true;
                existingSettings.EnableExport = true;
                existingSettings.EnableIntegrations = false;
                existingSettings.MaxStorageMB = 1000;
                existingSettings.MaxApiCallsPerDay = 1000;
                break;
                
            case "enterprise":
                existingSettings.MaxProjects = -1; // Unlimited
                existingSettings.MaxUsers = 50;
                existingSettings.EnableAdvancedFeatures = true;
                existingSettings.EnableApiAccess = true;
                existingSettings.EnableExport = true;
                existingSettings.EnableIntegrations = true;
                existingSettings.MaxStorageMB = 10000;
                existingSettings.MaxApiCallsPerDay = 5000;
                break;
                
            default:
                throw new ArgumentException($"Unknown plan: {plan}");
        }

        existingSettings.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(int tenantId, string plan, decimal amount)
    {
        // Calculate subscription dates with Indian time zone
        var (startDate, endDate) = CalculateSubscriptionDates();
        
        var subscription = new Subscription
        {
            TenantId = tenantId,
            Plan = plan,
            StartDate = startDate,
            EndDate = endDate,
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

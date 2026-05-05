using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Common.Interfaces;

[ApiController]
[Route("api/v1/subscription")]
[Authorize]
[EnableRateLimiting("tenant")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITenantContext _tenantContext;

    public SubscriptionController(ISubscriptionService subscriptionService, ITenantContext tenantContext)
    {
        _subscriptionService = subscriptionService;
        _tenantContext = tenantContext;
    }

    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionDto>> GetCurrentSubscription()
    {
        var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(_tenantContext.TenantId);
        
        if (subscription == null)
        {
            // Create a free plan subscription if none exists
            subscription = await _subscriptionService.CreateSubscriptionAsync(_tenantContext.TenantId, "Free", 0);
        }

        return Ok(subscription);
    }

    [HttpGet("plans")]
    public async Task<ActionResult<List<PlanDto>>> GetAvailablePlans()
    {
        var plans = await _subscriptionService.GetAvailablePlansAsync();
        return Ok(plans);
    }

    [HttpPost("upgrade")]
    public async Task<ActionResult<SubscriptionDto>> UpgradePlan([FromBody] UpgradePlanRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.UpgradePlanAsync(_tenantContext.TenantId, request.NewPlan);
            return Ok(subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("degrade")]
    public async Task<ActionResult<SubscriptionDto>> DegradePlan([FromBody] UpgradePlanRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.DegradePlanAsync(_tenantContext.TenantId, request.NewPlan);
            return Ok(subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<SubscriptionDto>>> GetSubscriptionHistory()
    {
        var history = await _subscriptionService.GetSubscriptionHistoryAsync(_tenantContext.TenantId);
        return Ok(history);
    }

    [HttpPost("cancel")]
    public async Task<ActionResult> CancelSubscription()
    {
        var success = await _subscriptionService.CancelSubscriptionAsync(_tenantContext.TenantId);
        
        if (!success)
        {
            return NotFound(new { message = "No active subscription found" });
        }

        return Ok(new { message = "Subscription cancelled successfully" });
    }
}

public record UpgradePlanRequest(string NewPlan);

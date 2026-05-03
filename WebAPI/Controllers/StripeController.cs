using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly IStripeService _stripeService;

    public StripeController(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    [HttpPost("create-checkout-session")]
    [Authorize("subscription.manage")]
    public async Task<ActionResult<string>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        try
        {
            var checkoutUrl = await _stripeService.CreateCheckoutSessionAsync(tenantId, request.PlanId);
            return Ok(new { checkoutUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        try
        {
            await _stripeService.ProcessWebhookAsync(json, signature);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success([FromQuery] string session_id)
    {
        // Handle successful payment
        return Redirect($"{Request.Scheme}://{Request.Host}/billing/success");
    }

    [HttpGet("cancel")]
    public IActionResult Cancel()
    {
        // Handle cancelled payment
        return Redirect($"{Request.Scheme}://{Request.Host}/billing/cancel");
    }
}

public record CreateCheckoutSessionRequest(string PlanId);

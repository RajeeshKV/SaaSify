using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly IStripePaymentService _stripePaymentService;

    public StripeController(IStripePaymentService stripePaymentService)
    {
        _stripePaymentService = stripePaymentService;
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
            var checkoutRequest = new CheckoutSessionRequest
            {
                TenantId = tenantId,
                PlanId = request.PlanId,
                CustomerEmail = request.CustomerEmail,
                Currency = "usd"
            };

            var checkoutSession = await _stripePaymentService.CreateCheckoutSessionAsync(checkoutRequest);
            return Ok(new { checkoutUrl = checkoutSession.CheckoutUrl });
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
            await _stripePaymentService.ProcessWebhookAsync(json, signature);
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

public record CreateCheckoutSessionRequest(string PlanId, string? CustomerEmail = null);

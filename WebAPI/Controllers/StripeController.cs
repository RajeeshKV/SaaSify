using Application.Stripe.Commands;
using Application.Stripe.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly IMediator _mediator;

    public StripeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("create-checkout-session")]
    [Authorize("subscription.manage")]
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant ID");
        }

        var command = new CreateCheckoutSessionCommand
        {
            TenantId = tenantId,
            PlanId = request.PlanId,
            CustomerEmail = request.CustomerEmail,
            Currency = "usd"
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        var command = new ProcessStripeWebhookCommand
        {
            JsonPayload = json,
            StripeSignature = signature
        };

        await _mediator.Send(command);
        return Ok();
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success([FromQuery] string session_id)
    {
        var query = new HandleStripeSuccessQuery { SessionId = session_id };
        var result = await _mediator.Send(query);

        if (result.Success)
        {
            return Redirect(result.RedirectUrl);
        }

        return BadRequest(result.Error);
    }

    [HttpGet("cancel")]
    public async Task<IActionResult> Cancel([FromQuery] string session_id)
    {
        var query = new HandleStripeCancelQuery { SessionId = session_id };
        var result = await _mediator.Send(query);

        if (result.Success)
        {
            return Redirect(result.RedirectUrl);
        }

        return BadRequest(result.Error);
    }
}

public record CreateCheckoutSessionRequest(string PlanId, string? CustomerEmail = null);

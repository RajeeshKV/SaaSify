using Application.Stripe.Commands;
using Application.Stripe.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly CreateCheckoutSessionCommandHandler _createCheckoutSessionHandler;
    private readonly ProcessStripeWebhookCommandHandler _processStripeWebhookHandler;
    private readonly HandleStripeSuccessQueryHandler _handleStripeSuccessQueryHandler;
    private readonly HandleStripeCancelQueryHandler _handleStripeCancelQueryHandler;
    private readonly IConfiguration _configuration;

    public StripeController(
        CreateCheckoutSessionCommandHandler createCheckoutSessionHandler,
        ProcessStripeWebhookCommandHandler processStripeWebhookHandler,
        HandleStripeSuccessQueryHandler handleStripeSuccessQueryHandler,
        HandleStripeCancelQueryHandler handleStripeCancelQueryHandler,
        IConfiguration configuration)
    {
        _createCheckoutSessionHandler = createCheckoutSessionHandler;
        _processStripeWebhookHandler = processStripeWebhookHandler;
        _handleStripeSuccessQueryHandler = handleStripeSuccessQueryHandler;
        _handleStripeCancelQueryHandler = handleStripeCancelQueryHandler;
        _configuration = configuration;
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

        var result = await _createCheckoutSessionHandler.Handle(command, default);
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

        await _processStripeWebhookHandler.Handle(command, default);
        return Ok();
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success([FromQuery] string session_id)
    {
        var query = new HandleStripeSuccessQuery { SessionId = session_id };
        var result = await _handleStripeSuccessQueryHandler.Handle(query, default);

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
        var result = await _handleStripeCancelQueryHandler.Handle(query, default);

        if (result.Success)
        {
            return Redirect(result.RedirectUrl);
        }

        return BadRequest(result.Error);
    }

    [HttpGet("order-success")]
    public async Task<IActionResult> OrderSuccess([FromQuery] string session_id)
    {
        var query = new HandleStripeSuccessQuery { SessionId = session_id };
        var result = await _handleStripeSuccessQueryHandler.Handle(query, default);

        if (result.Success)
        {
            // Redirect to frontend order success page
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://saasify.rajeesh.online";
            var redirectUrl = $"{frontendUrl}/order/success?session_id={session_id}&success=true";
            return Redirect(redirectUrl);
        }

        return BadRequest(result.Error);
    }

    [HttpGet("order-cancel")]
    public async Task<IActionResult> OrderCancel([FromQuery] string session_id)
    {
        var query = new HandleStripeCancelQuery { SessionId = session_id };
        var result = await _handleStripeCancelQueryHandler.Handle(query, default);

        if (result.Success)
        {
            // Redirect to frontend order cancel page
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://saasify.rajeesh.online";
            var redirectUrl = $"{frontendUrl}/order/cancel?session_id={session_id}&cancelled=true";
            return Redirect(redirectUrl);
        }

        return BadRequest(result.Error);
    }
}

public record CreateCheckoutSessionRequest(string PlanId, string? CustomerEmail = null);

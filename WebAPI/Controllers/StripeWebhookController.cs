using Microsoft.AspNetCore.Authorization;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;
using WebAPI.Services;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/stripe/webhook")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IStripePaymentService _stripePaymentService;
        private readonly OrderWebSocketService _webSocketService;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            IStripePaymentService stripePaymentService,
            OrderWebSocketService webSocketService,
            ILogger<StripeWebhookController> logger)
        {
            _stripePaymentService = stripePaymentService;
            _webSocketService = webSocketService;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var signature = Request.Headers["Stripe-Signature"];

                await _stripePaymentService.ProcessWebhookAsync(json, signature);

                _logger.LogInformation("Stripe webhook processed successfully");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Stripe webhook");
                return BadRequest("Webhook processing failed");
            }
        }
    }
}

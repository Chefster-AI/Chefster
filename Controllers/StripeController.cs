using Amazon.SQS;
using Amazon.SQS.Model;
using Chefster.Common;
using Chefster.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Chefster.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/stripe/callback")]
[ApiController]
public class StripeController(LoggingService loggingService, IConfiguration configuration)
    : ControllerBase
{
    private readonly LoggingService _logger = loggingService;
    private readonly AmazonSQSClient _amazonSQSClient = new();
    private readonly IConfiguration _configuration = configuration;

    [HttpPost]
    public async Task<IActionResult> HandleCallback()
    {
        var request = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var allowed = new HashSet<string>
        {
            EventTypes.InvoicePaymentSucceeded,
            EventTypes.CustomerSubscriptionCreated,
            EventTypes.CustomerSubscriptionUpdated,
            EventTypes.CheckoutSessionCompleted,
            EventTypes.ChargeFailed
        };

        try
        {
            var signatureSecret = _configuration["ASPNETCORE_ENVIRONMENT"] == "Development"
                ? _configuration["STRIPE_SIGNATURE_SECRET_DEV"]
                : _configuration["STRIPE_SIGNATURE_SECRET_PROD"];
            var stripeEvent = EventUtility.ConstructEvent(request, Request.Headers["Stripe-Signature"], signatureSecret);

            if (allowed.Contains(stripeEvent.Type))
            {
                await _amazonSQSClient.SendMessageAsync(
                    new SendMessageRequest
                    {
                        QueueUrl = _configuration["CALLBACK_QUEUE"],
                        MessageBody = request
                    }
                );
                _logger.Log($"[Queued] {stripeEvent.Type}", LogLevels.Info);
            }
            return Ok();
        }
        catch (Exception e)
        {
            _logger.Log($"Error receiving callback: {e}", LogLevels.Error);
            return BadRequest();
        }
    }
}

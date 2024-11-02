using Chefster.Common;
using Chefster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Stripe;

namespace Chefster.Controllers;

[Route("api/stripe/callback")]
[ApiController]
public class StripeCallbackController(LoggingService loggingService) : ControllerBase
{
    private readonly LoggingService _logger = loggingService;

    [HttpPost]
    public async Task<IActionResult> handleCallback()
    {
        var request = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        // Console.WriteLine(request);

        try
        {
            var stripeEvent = EventUtility.ParseEvent(request);

            Console.WriteLine($"Recieved this event Type: {stripeEvent.Type}");
            Console.WriteLine(request);
            Console.WriteLine(
                "********************************************************************"
            );

            // Here is where we would handle all the stripeEvents that can show up in a callback
            switch (stripeEvent.Type)
            {
                //Successful Cases
                case EventTypes.CheckoutSessionCompleted:
                case EventTypes.CustomerCreated:
                case EventTypes.CustomerUpdated:
                case EventTypes.ChargeUpdated:
                case EventTypes.ChargeSucceeded:
                case EventTypes.CustomerSubscriptionCreated:
                case EventTypes.CustomerSubscriptionUpdated:
                case EventTypes.PaymentIntentCreated:
                case EventTypes.PaymentIntentSucceeded:
                case EventTypes.InvoiceCreated:
                case EventTypes.InvoiceFinalized:
                case EventTypes.InvoiceUpdated:
                case EventTypes.InvoicePaid:
                case EventTypes.InvoicePaymentSucceeded:

                // Error Cases
                case EventTypes.ChargeFailed:
                case EventTypes.PaymentIntentPaymentFailed:
                case EventTypes.InvoicePaymentFailed:
                default:
                    _logger.Log("Received unhandled stripe callback", LogLevels.Warning);
                    break;
            }
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error receiving callback: {e}");
            return BadRequest();
        }
    }
}

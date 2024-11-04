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

            // Console.WriteLine($"Recieved this event Type: {stripeEvent.Type}");

            // Here is where we would handle all the stripeEvents that can show up in a callback
            switch (stripeEvent.Type)
            {
                //Successful Cases
                case EventTypes.CheckoutSessionCompleted:
                    var sessionComplete = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    Console.WriteLine(sessionComplete!.PaymentStatus);
                    break;
                case EventTypes.CustomerCreated:
                    var customer = stripeEvent.Data.Object as Customer;
                    Console.WriteLine(customer!.Id);
                    break;
                case EventTypes.CustomerUpdated:
                case EventTypes.ChargeUpdated:
                case EventTypes.ChargeSucceeded:
                case EventTypes.CustomerSubscriptionCreated:
                    var customerSubscription = stripeEvent.Data.Object as Subscription;
                    Console.WriteLine(customerSubscription!.Id);
                    break;
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
                    _logger.Log(
                        $"Received unhandled stripe callback {stripeEvent.Type}",
                        LogLevels.Warning
                    );
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

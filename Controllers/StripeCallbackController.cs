using Chefster.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Stripe;

namespace Chefster.Controllers;

[Route("api/stripe/callback")]
[ApiController]
public class StripeCallbackController : Controller
{
    [HttpPost]
    public async Task<IActionResult> handleCallback()
    {
        var request = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        // Console.WriteLine(request);

        try
        {
            var stripeEvent = EventUtility.ParseEvent(request);

            Console.WriteLine($"Recieved this event Type: {stripeEvent.Type}");

            // // Here is where we would handle all the stripeEvents that can show up in a callback
            // switch (stripeEvent.Type)
            // {
            //     case EventTypes.PaymentIntentSucceeded:
            //         Console.WriteLine("Payment Succeeded");
            //         break;
            //     case EventTypes.PaymentIntentCreated:
            //         Console.WriteLine(request);
            //         break;
            //     case EventTypes.ChargeSucceeded:
            //         Console.WriteLine(request);
            //         break;
            //     case EventTypes.ChargeUpdated:
            //         Console.WriteLine(request.ToJson());
            //         break;
            //     default:
            //         Console.WriteLine($"Recieved this event Type: {stripeEvent.Type}");
            //         break;
            // }
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error receiving callback: {e}");
            return BadRequest();
        }
    }
}

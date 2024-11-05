using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace Chefster.Controllers;

// [Route("api/stripe")]
[ApiController]
public class StripeController : Controller
{
    [HttpPost]
    [Route("create-checkout-session")]
    public ActionResult Create()
    {
        var domain = "http://localhost:5144";
        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = "price_1QHAIhGO5EdTXxQvjEjLCBXe",
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            CancelUrl = domain + "/account",
            SuccessUrl = domain + "/account",
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
        };
        var service = new SessionService();
        Session session = service.Create(options);

        Response.Headers.Append("Location", session.Url);
        return new StatusCodeResult(303);
    }

    [HttpGet]
    [Route("session-status")]
    public ActionResult SessionStatus([FromQuery] string session_id)
    {
        var sessionService = new SessionService();
        Session session = sessionService.Get(session_id);

        return Json(new {status = session.Status,  customer_email = session.CustomerDetails.Email});
    }
}
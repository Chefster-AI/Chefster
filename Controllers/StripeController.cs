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
            UiMode = "embedded",
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                Price = "price_1QHAIhGO5EdTXxQvjEjLCBXe",
                Quantity = 1,
                },
            },
            Mode = "subscription",
            ReturnUrl = domain + "/confirm?session_id={CHECKOUT_SESSION_ID}",
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
        };
        var service = new SessionService();
        Session session = service.Create(options);

        return Json(new {clientSecret = session.ClientSecret});
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
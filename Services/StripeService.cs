using Chefster.Common;
using Stripe;

namespace Chefster.Services;

public class StripeService(IConfiguration configuration)
{
    private readonly string? privateKey = configuration["STRIPE_TEST_KEY"];
    private readonly string? publishableKey = configuration["PUBLISHABLE_KEY"];
}

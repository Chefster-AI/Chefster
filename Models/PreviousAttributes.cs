namespace Chefster.Models;

public class PreviousAttributes
{
    public required long BillingCycleAnchor { get; set; }
    public required long CurrentPeriodEnd { get; set; }
    public required long CurrentPeriodStart { get; set; }
    public required string LatestInvoice { get; set; }
    public required string Status { get; set; }
    public required long TrialEnd { get; set; }
}

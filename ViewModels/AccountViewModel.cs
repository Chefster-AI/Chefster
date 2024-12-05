using Chefster.Common;

namespace Chefster.ViewModels;

public class AccountViewModel
{
    public required string FamilyId { get; set; }
    public required string Email { get; set; }
    public required UserStatus UserStatus { get; set; }
    public required DateTime JoinDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public required string StripePublishableKey { get; set; }
}
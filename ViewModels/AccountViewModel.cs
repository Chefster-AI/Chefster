using Chefster.Common;

namespace Chefster.ViewModels;

public class AccountViewModel
{
    public required string FamilyId { get; set; }
    public required string Email { get; set; }
    public required UserStatus UserStatus { get; set; }
    public required DateTime JoinDate { get; set; }
}
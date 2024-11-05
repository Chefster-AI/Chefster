using Chefster.Common;

namespace Chefster.ViewModels;

public class AccountViewModel
{
    public required string FamilyId { get; set; }
    public UserStatus UserStatus { get; set; }
}
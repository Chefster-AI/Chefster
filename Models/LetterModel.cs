namespace Chefster.Models;

public class LetterModel
{
    public required string Email { get; set; }
    public required FamilyModel Family { get; set; }
    public required AddressModel Address { get; set; }
}

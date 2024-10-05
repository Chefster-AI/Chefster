namespace Chefster.ViewModels;

public class FamilyViewModel
{
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public required int FamilySize { get; set; }
    public required int NumberOfBreakfastMeals { get; set; }
    public required int NumberOfLunchMeals { get; set; }
    public required int NumberOfDinnerMeals { get; set; }
    public required DayOfWeek GenerationDay { get; set; }
    public required TimeSpan GenerationTime { get; set; }
    public required string TimeZone { get; set; }
    public required List<MemberViewModel> Members { get; set; }
    public AddressViewModel? Address { get; set; }
}
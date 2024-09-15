using Chefster.Common;

namespace Chefster.ViewModels;

public class ThankYouViewModel
{
    public required string EmailAddress { get; set; }
    public required DayOfWeek GenerationDay { get; set; }
    public required TimeSpan GenerationTime { get; set; }
    public required UserStatus UserStatus { get; set; }
}
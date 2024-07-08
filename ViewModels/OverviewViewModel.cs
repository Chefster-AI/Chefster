using Chefster.Models;

namespace Chefster.ViewModels;

public class OverviewViewModel
{
    public required DayOfWeek GenerationDay { get; set; }
    public required TimeSpan GenerationTime { get; set; }
    public required List<PreviousRecipeModel> PreviousRecipes { get; set; }
}
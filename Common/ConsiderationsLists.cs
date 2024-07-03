using Microsoft.AspNetCore.Mvc.Rendering;

namespace Chefster.Common;

public static class ConsiderationsLists
{
    public static List<SelectListItem> RestrictionsList { get; } =
        new List<SelectListItem>
        {
            new SelectListItem { Text = "Carnivore", Selected = false },
            new SelectListItem { Text = "Gluten-Free", Selected = false },
            new SelectListItem { Text = "Nut-Free", Selected = false },
            new SelectListItem { Text = "Shellfish-Free", Selected = false },
            new SelectListItem { Text = "Vegan", Selected = false },
            new SelectListItem { Text = "Vegetarian", Selected = false }
        };

    public static List<SelectListItem> GoalsList { get; } =
        new List<SelectListItem>
        {
            new SelectListItem { Text = "Increase Energy", Selected = false },
            new SelectListItem { Text = "Maintain Weight", Selected = false },
            new SelectListItem { Text = "Mental Clarity", Selected = false },
            new SelectListItem { Text = "Muscle Gain", Selected = false },
            new SelectListItem { Text = "Overall Health", Selected = false },
            new SelectListItem { Text = "Weight Loss", Selected = false }
        };

    public static List<SelectListItem> CuisinesList { get; } =
        new List<SelectListItem>
        {
            new SelectListItem { Text = "American", Selected = false },
            new SelectListItem { Text = "Asian", Selected = false },
            new SelectListItem { Text = "Barbecue", Selected = false },
            new SelectListItem { Text = "Italian", Selected = false },
            new SelectListItem { Text = "Mexican", Selected = false },
            new SelectListItem { Text = "Seafood", Selected = false }
        };
}

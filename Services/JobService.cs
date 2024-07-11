using System.Text;
using Chefster.Models;
using Hangfire;
using static Chefster.Common.ConsiderationsEnum;

namespace Chefster.Services;

public class JobService(
    ConsiderationsService considerationsService,
    EmailService emailService,
    FamilyService familyService,
    GordonService gordonService,
    MemberService memberService,
    PreviousRecipesService previousRecipesService,
    ViewToStringService viewToStringService
)
{
    private readonly ConsiderationsService _considerationService = considerationsService;
    private readonly EmailService _emailService = emailService;
    private readonly FamilyService _familyService = familyService;
    private readonly GordonService _gordonService = gordonService;
    private readonly MemberService _memberService = memberService;
    private readonly PreviousRecipesService _previousRecipeService = previousRecipesService;
    private readonly ViewToStringService _viewToStringService = viewToStringService;

    /*
    The service is responsible for created and updating jobs that will
    gather gordons response and then send emails when the correct time comes
    */

    // Since hangfire has one function for creating and updating jobs this made the most sense
    public void CreateorUpdateEmailJob(string familyId)
    {
        var family = _familyService.GetById(familyId).Data;
        TimeZoneInfo timeZone;

        // create the job for the family
        if (family != null)
        {
            if (
                TimeZoneInfo.TryConvertIanaIdToWindowsId(
                    family.TimeZone,
                    out string? windowsTimeZoneId
                )
            )
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
            }
            else
            {
                timeZone = TimeZoneInfo.Utc;
            }

            var options = new RecurringJobOptions { TimeZone = timeZone };

            RecurringJob.AddOrUpdate(
                family.Id,
                () => GenerateAndSendRecipes(familyId),
                Cron.Weekly(
                    family.GenerationDay,
                    family.GenerationTime.Hours,
                    family.GenerationTime.Minutes
                ),
                options
            );
        }
    }

    public async Task GenerateAndSendRecipes(string familyId)
    {
        // grab family, get gordon's prompt, create the email, then send it
        var family = _familyService.GetById(familyId).Data;
        var gordonPrompt = BuildGordonPrompt(family!)!;
        var gordonResponse = await _gordonService.GetMessageResponse(gordonPrompt);
        var body = await _viewToStringService.ViewToStringAsync(
            "EmailTemplate",
            gordonResponse.Data!
        );

        if (family != null && body != null)
        {
            _emailService.SendEmail(family.Email, "Your weekly meal plan has arrived!", body);
        }

        var recipesToHold = ExtractRecipes(familyId, gordonResponse.Data!);
        int mealCount =
            family!.NumberOfBreakfastMeals + family.NumberOfLunchMeals + family.NumberOfDinnerMeals;
        _previousRecipeService.HoldRecipes(familyId, recipesToHold);
        _previousRecipeService.RealeaseRecipes(familyId, mealCount);
    }

    public string BuildGordonPrompt(FamilyModel family)
    {
        string mealCounts = GetMealCountsText(
            family.NumberOfBreakfastMeals,
            family.NumberOfLunchMeals,
            family.NumberOfDinnerMeals
        );
        string dietaryConsiderations = GetDietaryConsiderationsText(family.Id);
        string previousRecipes = GetPreviousRecipesText(family.Id);
        string gordonPrompt =
            $"Create {mealCounts} recipes, each being {family.FamilySize} servings. Here is a list of the dietary considerations:\n{dietaryConsiderations}{previousRecipes}";
        Console.WriteLine("Gordon's Prompt:\n" + gordonPrompt);
        return gordonPrompt;
    }

    private string GetMealCountsText(
        int numberOfBreakfastMeals,
        int numberOfLunchMeals,
        int numberOfDinnerMeals
    )
    {
        List<string> mealCounts = new List<string>();
        string mealCountsText = "";

        if (numberOfBreakfastMeals > 0)
        {
            mealCounts.Add($"{numberOfBreakfastMeals} breakfast");
        }
        if (numberOfLunchMeals > 0)
        {
            mealCounts.Add($"{numberOfLunchMeals} lunch");
        }
        if (numberOfDinnerMeals > 0)
        {
            mealCounts.Add($"{numberOfDinnerMeals} dinner");
        }

        switch (mealCounts.Count)
        {
            // maybe through an error? this case only occurs if the user wants 0 breakfast, lunch, and dinner recipes
            case 0:
                mealCountsText = "no";
                break;
            case 1:
                mealCountsText = mealCounts[0];
                break;
            case 2:
                mealCountsText = $"{mealCounts[0]} and {mealCounts[1]}";
                break;
            case 3:
                mealCountsText = $"{mealCounts[0]}, {mealCounts[1]}, and {mealCounts[2]}";
                break;
        }

        return mealCountsText;
    }

    private string GetDietaryConsiderationsText(string familyId)
    {
        var considerationsText = new StringBuilder();

        var result = _memberService.GetByFamilyId(familyId);

        if (result.Success)
        {
            var members = result.Data;
            foreach (var member in members!)
            {
                List<string> restrictions = new List<string>();
                List<string> goals = new List<string>();
                List<string> cuisines = new List<string>();
                var result2 = _considerationService.GetMemberConsiderations(member.MemberId);

                if (result2.Success)
                {
                    var memberConsiderations = result2.Data;

                    foreach (var consideration in memberConsiderations!)
                    {
                        switch (consideration.Type)
                        {
                            case Restriction:
                                restrictions.Add(consideration.Value);
                                break;
                            case Goal:
                                goals.Add(consideration.Value);
                                break;
                            case Cuisine:
                                cuisines.Add(consideration.Value);
                                break;
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }

                var memberConsiderationsText = string.Join("\n", new[]
                    {
                        $"Name: {member.Name}",
                        !string.IsNullOrEmpty(member.Notes) ? $"Notes: {member.Notes}" : null,
                        restrictions.Any() ? $"Restrictions: {string.Join(", ", restrictions)}" : null,
                        goals.Any() ? $"Goals: {string.Join(", ", goals)}" : null,
                        cuisines.Any() ? $"Favorite Cuisines: {string.Join(", ", cuisines)}" : null
                    }.Where(s => s != null)) + "\n\n";
                
                considerationsText.Append(memberConsiderationsText);
            }

            return considerationsText.ToString();
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private string GetPreviousRecipesText(string familyId)
    {
        var previousRecipes = _previousRecipeService.GetPreviousRecipes(familyId).Data!;
        var enjoyed = new List<string>();
        var notEnjoyed = new List<string>();

        foreach (var recipe in previousRecipes)
        {
            if (recipe.Enjoyed != null)
            {
                ((bool)recipe.Enjoyed ? enjoyed : notEnjoyed).Add(recipe.DishName);
            }
            else
            {
                notEnjoyed.Add(recipe.DishName);
            }
        }

        return string.Join("\n\n", new[]
            {
                enjoyed.Any() ? "Generate recipes that are similar to the ones listed here, but be certain that you generate different recipes:\n" + string.Join(", ", enjoyed) : null,
                notEnjoyed.Any() ? "Do not generate recipes these recipes, or recipes that are similar to the ones listed here:\n" + string.Join(", ", notEnjoyed) : null
            }.Where(s => s != null));
    }

    private List<PreviousRecipeCreateDto> ExtractRecipes(
        string familyId,
        GordonResponseModel gordonResponse
    )
    {
        var recipes = new List<PreviousRecipeCreateDto>();

        if (gordonResponse.BreakfastRecipes != null)
        {
            foreach (var recipe in gordonResponse.BreakfastRecipes)
            {
                recipes.Add(
                    new PreviousRecipeCreateDto
                    {
                        FamilyId = familyId,
                        DishName = recipe.DishName,
                        MealType = "Breakfast"
                    }
                );
            }
        }

        if (gordonResponse.LunchRecipes != null)
        {
            foreach (var recipe in gordonResponse.LunchRecipes)
            {
                recipes.Add(
                    new PreviousRecipeCreateDto
                    {
                        FamilyId = familyId,
                        DishName = recipe.DishName,
                        MealType = "Lunch"
                    }
                );
            }
        }

        if (gordonResponse.DinnerRecipes != null)
        {
            foreach (var recipe in gordonResponse.DinnerRecipes)
            {
                recipes.Add(
                    new PreviousRecipeCreateDto
                    {
                        FamilyId = familyId,
                        DishName = recipe.DishName,
                        MealType = "Dinner"
                    }
                );
            }
        }

        return recipes;
    }
}

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
        The service is responsible for created, updating and executing jobs that will
        gather gordons response and then send emails when the correct time comes
    */

    // Since hangfire has one function for creating and updating jobs we are using one function here for that
    public void CreateorUpdateEmailJob(string familyId)
    {
        var family = _familyService.GetById(familyId).Data;
        TimeZoneInfo timeZone;

        // create the job for the family
        if (family != null)
        {
            // attempt to get user time zone
            var timeZoneConversion = TimeZoneInfo.TryConvertIanaIdToWindowsId(
                family.TimeZone,
                out string? windowsTimeZoneId
            );
            if (timeZoneConversion && windowsTimeZoneId != null)
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
            }
            else
            {
                // default to UTC if we can't get it
                timeZone = TimeZoneInfo.Utc;
            }

            // set time zone and update/create job
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
        if (family != null)
        {
            var gordonPrompt = BuildGordonPrompt(family!)!;
            var gordonResponse = await _gordonService.GetMessageResponse(gordonPrompt);
            var body = await _viewToStringService.ViewToStringAsync(
                "EmailTemplate",
                gordonResponse.Data!
            );

            if (body != null)
            {
                _emailService.SendEmail(family.Email, "Your weekly meal plan has arrived!", body);
            }

            var recipesToHold = ExtractRecipes(familyId, gordonResponse.Data!);
            int mealCount =
                family!.NumberOfBreakfastMeals
                + family.NumberOfLunchMeals
                + family.NumberOfDinnerMeals;
            _previousRecipeService.HoldRecipes(familyId, recipesToHold);
            _previousRecipeService.RealeaseRecipes(familyId, mealCount);
        }
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

    private static string GetMealCountsText(
        int numberOfBreakfastMeals,
        int numberOfLunchMeals,
        int numberOfDinnerMeals
    )
    {
        List<string> mealCounts = [];
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

        var members = _memberService.GetByFamilyId(familyId).Data;

        if (members != null)
        {
            foreach (var member in members)
            {
                List<string> restrictions = [];
                List<string> goals = [];
                List<string> cuisines = [];

                var memberConsiderations = _considerationService
                    .GetMemberConsiderations(member.MemberId)
                    .Data;

                if (memberConsiderations != null)
                {
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
                    throw new Exception("Member considerations list was null rather than empty");
                }

                // Is it acceptable to send empty lists to Gordon if the Member didn't select any preferences?
                var memberConsiderationsText =
                    $"Name: {member.Name}\nNotes: {member.Notes}\nRestrictions: {string.Join(", ", restrictions)}\nGoals: {string.Join(", ", goals)}\nFavorite Cuisines: {string.Join(", ", cuisines)}\n";
                considerationsText.Append(memberConsiderationsText);
            }

            return considerationsText.ToString();
        }
        else
        {
            throw new Exception("Members list was null rather than empty");
        }
    }

    private string GetPreviousRecipesText(string familyId)
    {
        // a list of recipes if they exist otherwise []
        var previousRecipes = _previousRecipeService.GetPreviousRecipes(familyId).Data!;

        List<string> enjoyedBreakfast = [];
        List<string> enjoyedLunch = [];
        List<string> enjoyedDinner = [];
        List<string> indifferentBreakfast = [];
        List<string> indifferentLunch = [];
        List<string> indifferentDinner = [];
        List<string> notEnjoyedBreakfast = [];
        List<string> notEnjoyedLunch = [];
        List<string> notEnjoyedDinner = [];

        foreach (var recipe in previousRecipes)
        {
            if (recipe.Enjoyed != null)
            {
                switch (recipe.MealType)
                {
                    case "Breakfast":
                        ((bool)recipe.Enjoyed ? enjoyedBreakfast : notEnjoyedBreakfast).Add(
                            recipe.DishName
                        );
                        break;
                    case "Lunch":
                        ((bool)recipe.Enjoyed ? enjoyedLunch : notEnjoyedLunch).Add(
                            recipe.DishName
                        );
                        break;
                    case "Dinner":
                        ((bool)recipe.Enjoyed ? enjoyedDinner : notEnjoyedDinner).Add(
                            recipe.DishName
                        );
                        break;
                }
            }
            else
            {
                switch (recipe.MealType)
                {
                    case "Breakfast":
                        indifferentBreakfast.Add(recipe.DishName);
                        break;
                    case "Lunch":
                        indifferentLunch.Add(recipe.DishName);
                        break;
                    case "Dinner":
                        indifferentDinner.Add(recipe.DishName);
                        break;
                }
            }
        }

        // this may need reworded
        return $@"
Generate recipes that are similar to the ones listed here, but be certain that you generate different recipes:
Breakfast: {string.Join(", ", enjoyedBreakfast)}
Lunch: {string.Join(", ", enjoyedLunch)}
Dinner: {string.Join(", ", enjoyedDinner)}

Do not generate recipes these recipes, or recipes that are similar to the ones listed here:
Breakfast: {string.Join(", ", notEnjoyedBreakfast)}
Lunch: {string.Join(", ", notEnjoyedLunch)}
Dinner: {string.Join(", ", notEnjoyedDinner)}
        ";
    }

    private static List<PreviousRecipeCreateDto> ExtractRecipes(
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

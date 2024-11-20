using System.Text;
using Chefster.Common;
using Chefster.Models;
using Hangfire;
using static Chefster.Common.ConsiderationsEnum;
using static Chefster.Common.Helpers;
using static Chefster.Common.Constants;

namespace Chefster.Services;

public class JobService(
    ConsiderationsService considerationsService,
    EmailService emailService,
    FamilyService familyService,
    GordonService gordonService,
    JobRecordService jobRecordService,
    MemberService memberService,
    PreviousRecipesService previousRecipesService,
    ViewToStringService viewToStringService,
    LoggingService loggingService,
    IConfiguration configuration
)
{
    private readonly ConsiderationsService _considerationService = considerationsService;
    private readonly EmailService _emailService = emailService;
    private readonly FamilyService _familyService = familyService;
    private readonly GordonService _gordonService = gordonService;
    private readonly JobRecordService _jobRecordService = jobRecordService;
    private readonly MemberService _memberService = memberService;
    private readonly PreviousRecipesService _previousRecipeService = previousRecipesService;
    private readonly ViewToStringService _viewToStringService = viewToStringService;
    private readonly LoggingService _logger = loggingService;
    private readonly IConfiguration _configuration = configuration;

    /*
        The service is responsible for created, updating and executing jobs that will
        gather gordons response and then send emails when the correct time comes
    */

    // Since hangfire has one function for creating and updating jobs we are using one function here for that
    // Obsolute tag suppresses the warning for QueueName
    public void CreateOrUpdateJob(string familyId)
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

            string queueName =
                _configuration["ASPNETCORE_ENVIRONMENT"] == "Development"
                    ? _configuration["QUEUE_NAME"]!
                    : "default";

            // set time zone, queue name, and update/create job
            var options = new RecurringJobOptions { TimeZone = timeZone };
            RecurringJob.AddOrUpdate(
                recurringJobId: family.Id,
                methodCall: () => GenerateAndSendRecipes(familyId),
                cronExpression: Cron.Weekly(
                    family.GenerationDay,
                    family.GenerationTime.Hours,
                    family.GenerationTime.Minutes
                ),
                queue: queueName,
                options: options
            );
            _logger.Log(
                $"Created or updated job with Id: {family.Id}. Added to Queue: {queueName}. Generation Time: {family.GenerationDay} {family.GenerationTime.Hours}:{family.GenerationTime.Minutes}",
                LogLevels.Info,
                "CreateOrUpdateEmailJob"
            );
            return;
        }
    }

    public async Task GenerateAndSendRecipes(string familyId)
    {
        var family = _familyService.GetById(familyId).Data;
        DateTime startTime = GetUserCurrentTime(family!.TimeZone);

        if (family != null)
        {
            // Ensure job run meets cooldown period
            if (family!.JobTimestamp != null)
            {
                DateTime familyCurrentTime = GetUserCurrentTime(family.TimeZone);
                double daysSinceLastRun = (familyCurrentTime - family.JobTimestamp).Value.TotalDays;
                Console.WriteLine("Last Job Run Timestamp: " + family.JobTimestamp.ToString()!);
                Console.WriteLine("Family's current time: " + familyCurrentTime.ToString());
                Console.WriteLine("Days difference: " + daysSinceLastRun);

                if (daysSinceLastRun < JOB_COOLDOWN_DAYS)
                {
                    Console.WriteLine("In Cooldown.");
                    _logger.Log(
                        $"Family with ID {family.Id} is now in a cooldown.  Days Difference: {daysSinceLastRun}",
                        LogLevels.Info,
                        "GenerateAndSendRecipe"
                    );
                    return;
                }
            }

            // Build the prompt and send it to Gordon
            var gordonPrompt = BuildGordonPrompt(family!)!;
            var gordonResponse = await _gordonService.GetMessageResponse(gordonPrompt);

            // Create the email and send it to the user
            var body = await _viewToStringService.ViewToStringAsync(
                "EmailTemplate",
                gordonResponse.Data!
            );
            if (body != null)
            {
                _emailService.SendEmail(family.Email, "Your weekly meal plan has arrived!", body);
                _logger.Log(
                    $"Successfully send recipe email for family with ID: {family.Id} and Email: {family.Email}",
                    LogLevels.Info
                );
            }
            else
            {
                _logger.Log(
                    $"Body for email was NULL. Attempted to send email to family with ID: {family.Id} and Email: {family.Email}",
                    LogLevels.Error
                );
                throw new Exception("Body for recipe email was null");
            }
            
            // Store previous recipes to reduce redundant suggestions
            var recipesToHold = ExtractRecipes(familyId, gordonResponse.Data!);
            int mealCount =
                family!.NumberOfBreakfastMeals
                + family.NumberOfLunchMeals
                + family.NumberOfDinnerMeals;
            var holdSuccess = _previousRecipeService.HoldRecipes(
                familyId,
                family.TimeZone,
                recipesToHold
            );
            if (!holdSuccess.Success)
            {
                _logger.Log(
                    $"Error Holding recipes for family with ID: {family.Id} and Email: {family.Email}. Error: {holdSuccess.Error}",
                    LogLevels.Error
                );
            }

            // Release previously stored recipes to allow them to be suggested again
            var releaseSuccess = _previousRecipeService.RealeaseRecipes(familyId, mealCount);
            if (!releaseSuccess.Success)
            {
                _logger.Log(
                    $"Error releasing recipes for family with ID: {family.Id} and Email: {family.Email}. Error: {releaseSuccess.Error}",
                    LogLevels.Error
                );
            }

            // Timestamp the job
            family = _familyService.UpdateFamilyJobTimestamp(
                familyId,
                TimeZoneInfo.ConvertTime(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById(family.TimeZone)
                )
            ).Data;
            _logger.Log(
                $"Updated JobTimestamp for family with ID: {family.Id} and Email: {family.Email}",
                LogLevels.Info
            );

            // Record job details
            var jobRecord = new JobRecordCreateDto
            {
                FamilyId = family.Id,
                StartTime = startTime,
                EndTime = GetUserCurrentTime(family.TimeZone),
                JobStatus = JobStatus.Completed,
                JobType = JobType.RecipeGeneration
            };
            _jobRecordService.CreateJobRecord(jobRecord);

            // Remove recurring job if family is no longer either in FreeTrial or Subscribed
            if (family.UserStatus != UserStatus.FreeTrial || family.UserStatus != UserStatus.Subscribed)
            {
                RecurringJob.RemoveIfExists(family.Id);
            }
        }
        else
        {
            _logger.Log(
                $"Family was null when attempting to generate and send recipe email. ID: {familyId}",
                LogLevels.Error
            );
            var jobRecord = new JobRecordCreateDto
            {
                FamilyId = family.Id,
                StartTime = startTime,
                EndTime = GetUserCurrentTime(family.TimeZone),
                JobStatus = JobStatus.Failed,
                JobType = JobType.RecipeGeneration
            };
            _jobRecordService.CreateJobRecord(jobRecord);
            throw new Exception(
                "Family was null when attempting to generate and send recipe email"
            );
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

                var memberConsiderationsText =
                    string.Join(
                        "\n",
                        new[]
                        {
                            $"Name: {member.Name}",
                            !string.IsNullOrEmpty(member.Notes) ? $"Notes: {member.Notes}" : null,
                            restrictions.Any()
                                ? $"Restrictions: {string.Join(", ", restrictions)}"
                                : null,
                            goals.Any() ? $"Goals: {string.Join(", ", goals)}" : null,
                            cuisines.Any()
                                ? $"Favorite Cuisines: {string.Join(", ", cuisines)}"
                                : null
                        }.Where(s => s != null)
                    ) + "\n\n";

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

        return string.Join(
            "\n\n",
            new[]
            {
                enjoyed.Any()
                    ? "Generate recipes that are similar to the ones listed here, but be certain that you generate different recipes:\n"
                        + string.Join(", ", enjoyed)
                    : null,
                notEnjoyed.Any()
                    ? "Do not generate these recipes, or recipes that are similar to the ones listed here:\n"
                        + string.Join(", ", notEnjoyed)
                    : null
            }.Where(s => s != null)
        );
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

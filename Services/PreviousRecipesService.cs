using Chefster.Common;
using Chefster.Context;
using Chefster.Models;
using Microsoft.Data.SqlClient;

namespace Chefster.Services;

public class PreviousRecipesService(ChefsterDbContext context, LoggingService loggingService)
{
    private readonly ChefsterDbContext _context = context;
    private readonly LoggingService _logger = loggingService;

    public ServiceResult<List<PreviousRecipeModel>> GetPreviousRecipes(string familyId)
    {
        var previousRecipes = new List<PreviousRecipeModel>();
        try
        {
            previousRecipes = [.. _context.PreviousRecipes.Where(e => e.FamilyId == familyId)];
            return ServiceResult<List<PreviousRecipeModel>>.SuccessResult(previousRecipes);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<PreviousRecipeModel>>.ErrorResult(
                $"Failed to get previous recipes for family {familyId}. Error: {e}",
                _logger
            );
        }
    }

    public ServiceResult<PreviousRecipeModel> UpdatePreviousRecipe(
        PreviousRecipeUpdateDto previousRecipe
    )
    {
        try
        {
            var existingPreviousRecipe = _context.PreviousRecipes.Find(previousRecipe.RecipeId);
            if (existingPreviousRecipe == null)
            {
                return ServiceResult<PreviousRecipeModel>.ErrorResult(
                    $"Failed to find previous recipe with Id: {previousRecipe.RecipeId}",
                    _logger
                );
            }

            existingPreviousRecipe.Enjoyed = previousRecipe.Enjoyed;
            _context.SaveChanges();

            return ServiceResult<PreviousRecipeModel>.SuccessResult(existingPreviousRecipe);
        }
        catch (SqlException e)
        {
            return ServiceResult<PreviousRecipeModel>.ErrorResult(
                $"Failed to update previous recipe with Id: {previousRecipe.RecipeId}. Error: {e}",
                _logger
            );
        }
    }

    public ServiceResult<Task> HoldRecipes(
        string familyId,
        string familyTimeZone,
        List<PreviousRecipeCreateDto> recipesToHold
    )
    {
        try
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(familyTimeZone);

            foreach (var recipe in recipesToHold)
            {
                var previousRecipe = new PreviousRecipeModel
                {
                    RecipeId = Guid.NewGuid().ToString("N"),
                    FamilyId = recipe.FamilyId,
                    DishName = recipe.DishName,
                    MealType = recipe.MealType,
                    Enjoyed = null,
                    CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo),
                };

                _context.PreviousRecipes.Add(previousRecipe);
                _context.SaveChanges();
            }

            return ServiceResult<Task>.SuccessResult(Task.CompletedTask);
        }
        catch (SqlException e)
        {
            return ServiceResult<Task>.ErrorResult(
                $"Failed to hold previous recipes for family {familyId}. Error: {e}",
                _logger
            );
        }
    }

    public ServiceResult<Task> RealeaseRecipes(string familyId, int mealCount)
    {
        try
        {
            var newestEntries = _context
                .PreviousRecipes.Where(e => e.FamilyId == familyId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(GetNumberOfPreviousRecipes(mealCount))
                .Select(e => e.RecipeId)
                .ToList();

            var entriesToDelete = _context
                .PreviousRecipes.Where(e =>
                    e.FamilyId == familyId && !newestEntries.Contains(e.RecipeId)
                )
                .ToList();

            _context.PreviousRecipes.RemoveRange(entriesToDelete);
            _context.SaveChanges();

            return ServiceResult<Task>.SuccessResult(Task.CompletedTask);
        }
        catch (SqlException e)
        {
            return ServiceResult<Task>.ErrorResult(
                $"Failed to release previous recipes for family {familyId}. Error: {e}",
                _logger
            );
        }
    }

    public int GetNumberOfPreviousRecipes(int mealCount)
    {
        return Math.Max(10, mealCount * 2);
    }
}

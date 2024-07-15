using Chefster.Common;
using Chefster.Context;
using Chefster.Models;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;

namespace Chefster.Services;

public class PreviousRecipesService(ChefsterDbContext context)
{
    private readonly ChefsterDbContext _context = context;

    public ServiceResult<List<PreviousRecipeModel>> GetPreviousRecipes(string familyId)
    {
        var previousRecipes = new List<PreviousRecipeModel>();
        try
        {
            previousRecipes = _context.PreviousRecipes
                .Where(e => e.FamilyId == familyId)
                .ToList();
            
            return ServiceResult<List<PreviousRecipeModel>>.SuccessResult(previousRecipes);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<PreviousRecipeModel>>.ErrorResult(
                $"Failed to get previous recipes for family {familyId}. Error: {e}"
            );
        }
    }

    public Task UpdatePreviousRecipe(PreviousRecipeUpdateDto previousRecipe)
    {
        try
        {
            var existingPreviousRecipe = _context.PreviousRecipes.Find(previousRecipe.RecipeId);
            if (existingPreviousRecipe == null)
            {
                return Task.FromException(new Exception("Failed to find previous recipe " + previousRecipe.RecipeId));
            }
            
            existingPreviousRecipe.Enjoyed = previousRecipe.Enjoyed;
            _context.SaveChanges();
            
            return Task.CompletedTask;
        }
        catch (SqlException e)
        {
            return Task.FromException(new Exception("Failed to update previous recipe " + previousRecipe.ToJson()));
        }
    }

    public ServiceResult<string> HoldRecipes(string familyId, string familyTimeZone, List<PreviousRecipeCreateDto> recipesToHold)
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
            
            return ServiceResult<string>.SuccessResult("Successfully inserted previous recipes!");
        }
        catch (SqlException e)
        {
            return ServiceResult<string>.ErrorResult(
                $"Failed to hold previous recipes for family {familyId}. Error: {e}"
            );
        }
    }

    public ServiceResult<string> RealeaseRecipes(string familyId, int mealCount)
    {
        try
        {
            var newestEntries = _context.PreviousRecipes
                .Where(e => e.FamilyId == familyId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(GetNumberOfPreviousRecipes(mealCount))
                .Select(e => e.RecipeId)
                .ToList();

            var entriesToDelete = _context.PreviousRecipes
                .Where(e => e.FamilyId == familyId && !newestEntries.Contains(e.RecipeId))
                .ToList();

            _context.PreviousRecipes.RemoveRange(entriesToDelete);
            _context.SaveChanges();

            return ServiceResult<string>.SuccessResult(familyId);
        }
        catch (SqlException e)
        {
            return ServiceResult<string>.ErrorResult(
                $"Failed to release previous recipes for family {familyId}. Error: {e}"
            );
        }
    }

    public int GetNumberOfPreviousRecipes(int mealCount)
    {
        return Math.Max(10, mealCount * 2);
    }
}
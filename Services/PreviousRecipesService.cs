using Chefster.Common;
using static Chefster.Common.Constants;
using Chefster.Context;
using Chefster.Models;
using Microsoft.Data.SqlClient;

namespace Chefster.Services;

public class PreviousRecipesService(ChefsterDbContext context)
{
    private readonly ChefsterDbContext _context = context;

    public ServiceResult<List<string>> GetPreviousRecipes(string familyId)
    {
        var previousRecipes = new List<string>();
        try
        {
            previousRecipes = _context.PreviousRecipes
                .Where(e => e.FamilyId == familyId)
                .Select(e => e.DishName.ToString())
                .ToList();
            
            return ServiceResult<List<string>>.SuccessResult(previousRecipes);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<string>>.ErrorResult(
                $"Failed to get previous recipes for family {familyId}. Error: {e}"
            );
        }
    }

    public ServiceResult<string> HoldRecipes(string familyId, List<string> dishNames)
    {
        try
        {
            foreach (var dishName in dishNames)
            {
                var previousRecipe = new PreviousRecipeModel
                {
                    RecipeId = Guid.NewGuid().ToString("N"),
                    FamilyId = familyId,
                    DishName = dishName,
                    CreatedAt = DateTime.UtcNow
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

        return ServiceResult<string>.ErrorResult($"Failed to hold previous recipes for family {familyId}.");
    }

    public ServiceResult<string> RealeaseRecipes(string familyId)
    {
        try
        {
            var newestEntries = _context.PreviousRecipes
                .Where(e => e.FamilyId == familyId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(KEEP_N_PREVIOUS_RECIPES)
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
}
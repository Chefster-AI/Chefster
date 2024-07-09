using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("PreviousRecipes")]
[PrimaryKey(nameof(RecipeId))]
public class PreviousRecipeModel
{
    public required string RecipeId { get; set; }
    public required string FamilyId { get; set; }
    public required string DishName { get; set; }
    public required string MealType { get; set; }
    public bool? Enjoyed { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class PreviousRecipeCreateDto
{
    public required string FamilyId { get; set; }
    public required string DishName { get; set; }
    public required string MealType { get; set; }
}
using Newtonsoft.Json;

namespace Chefster.Models;

public class GordonResponseModel
{
    [JsonProperty("groceryList")]
    public required List<string> GroceryList { get; set; }

    [JsonProperty("notes")]
    public required string Notes { get; set; }

    [JsonProperty("breakfastRecipes")]
    public List<Recipe>? BreakfastRecipes { get; set; }

    [JsonProperty("lunchRecipes")]
    public List<Recipe>? LunchRecipes { get; set; }

    [JsonProperty("dinnerRecipes")]
    public List<Recipe>? DinnerRecipes { get; set; }


    public class Recipe
    {
        [JsonProperty("dishName")]
        public required string DishName { get; set; }

        [JsonProperty("ingredients")]
        public required List<string> Ingredients { get; set; }

        [JsonProperty("instructions")]
        public required List<string> Instructions { get; set; }

        [JsonProperty("prepareTime")]
        public required string PrepareTime { get; set; }

        [JsonProperty("servings")]
        public required int Servings { get; set; }
    }
}
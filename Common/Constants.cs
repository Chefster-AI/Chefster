using Chefster.Models;

namespace Chefster.Common;

public class Constants
{
    public const int MAX_MEMBERS = 10;
    public const int MAX_NOTES = 10;
    public const int JOB_COOLDOWN_DAYS = 3;
    public static GordonResponseModel GORDON_RESPONSE_EXAMPLE = new GordonResponseModel
    {
        Notes = "All recipes are 4 servings and nut-free to accommodate Joe's restriction.",
        BreakfastRecipes = [
            new GordonResponseModel.Recipe
            {
                DishName = "Veggie Breakfast Burritos",
                PrepareTime = "20 minutes",
                Servings = 4,
                Ingredients = ["4 eggs", "1 cup spinach", "1 cup cherry tomatoes", "1 avocado", "4 whole wheat tortillas", "1/2 cup shredded cheese"],
                Instructions = ["Whisk the eggs in a bowl and scramble them in a non-stick skillet over medium heat.", "Add chopped spinach and halved cherry tomatoes to the skillet, cooking until the spinach is wilted.", "Slice the avocado.", "Divide the egg mixture evenly among the tortillas.", "Top each tortilla with sliced avocado and shredded cheese.", "Roll up the tortillas and serve warm."]
            },
            new GordonResponseModel.Recipe
            {
                DishName = "Smoked Salmon Toast",
                PrepareTime = "15 minutes",
                Servings = 4,
                Ingredients = ["4 slices smoked salmon", "1 avocado", "1 cucumber", "4 whole wheat rolls"],
                Instructions = ["Toast the whole wheat rolls.", "Mash the avocado in a bowl.", "Spread the mashed avocado over the toasted rolls.", "Top each roll with slices of smoked salmon.", "Thinly slice the cucumber and place on top of the salmon and serve."]
            }
        ],
        LunchRecipes = [
            new GordonResponseModel.Recipe
            {
                DishName = "Quinoa Chicken Salad",
                PrepareTime = "30 minutes",
                Servings = 4,
                Ingredients = ["1 cup quinoa", "2 chicken breasts", "1 red bell pepper", "1/2 onion", "2 cups mixed greens"],
                Instructions = ["Cook the quinoa according to the package instructions and let it cool.", "Season and grill the chicken breasts until fully cooked. Let them rest before slicing.", "Dice the red bell pepper and onion.", "In a large bowl, combine the quinoa, diced red bell pepper, diced onion, and mixed greens.", "Slice the chicken breasts and place on top of the salad."]
            },
            new GordonResponseModel.Recipe
            {
                DishName = "Beef Taco Bowls",
                PrepareTime = "25 minutes",
                Servings = 4,
                Ingredients = ["1 lb ground beef", "1 can diced tomatoes", "1 cup corn kernels", "1 packet taco seasoning", "1 cup shredded lettuce", "4 whole wheat tortillas"],
                Instructions = ["Cook the ground beef in a skillet until browned. Drain any excess fat.", "Add the diced tomatoes, corn kernels, and taco seasoning to the skillet. Stir to combine and simmer for 10 minutes.", "Warm the tortillas in a dry skillet or microwave.", "Fill each tortilla with the beef mixture and top with shredded lettuce.", "Serve the taco bowls immediately."]
            }
        ],
        DinnerRecipes = [
            new GordonResponseModel.Recipe
            {
                DishName = "BBQ Cheeseburgers",
                PrepareTime = "25 minutes",
                Servings = 4,
                Ingredients = ["1 lb ground beef", "4 burger buns", "4 cheddar cheese slices", "1/2 cup BBQ sauce", "1 cup shredded lettuce"],
                Instructions = ["Form the ground beef into 4 patties.", "Grill the patties over medium heat until they reach the desired doneness.", "Top each patty with a slice of cheddar cheese and let it melt.", "Toast the burger buns on the grill.", "Spread BBQ sauce on both sides of each bun.", "Assemble the burgers by placing the patties on the buns and topping with shredded lettuce."]
            },
            new GordonResponseModel.Recipe
            {
                DishName = "Lemon Herb Salmon with Asparagus",
                PrepareTime = "30 minutes",
                Servings = 4,
                Ingredients = ["1 lb salmon fillet", "1 lemon", "1 pack asparagus", "1 cup rice"],
                Instructions = ["Preheat your oven to 400°F (200°C).", "Place the salmon fillet on a baking sheet lined with parchment paper.", "Squeeze the juice of one lemon over the salmon and season with salt and pepper.", "Trim the asparagus and place them around the salmon on the baking sheet.", "Roast in the oven for 15-20 minutes, or until the salmon is cooked through.", "Meanwhile, cook the rice according to the package instructions.", "Serve the salmon and asparagus over a bed of rice."]
            }
        ],
        GroceryList = ["4 eggs", "1 cup spinach", "1 cup cherry tomatoes", "1 avocado", "4 whole wheat tortillas", "1/2 cup shredded cheese", "4 slices smoked salmon", "1 cucumber", "1/2 onion", "1 cup quinoa", "2 chicken breasts", "1 red bell pepper", "2 cups mixed greens", "4 whole wheat rolls", "1 lb ground beef", "1 can diced tomatoes", "1 cup corn kernels", "1 packet taco seasoning", "1 cup shredded lettuce", "4 burger buns", "4 cheddar cheese slices", "1/2 cup BBQ sauce", "1 lb pasta", "1 cup tomato sauce", "1/2 cup grated parmesan cheese", "1 cup fresh basil", "1 lb salmon fillet", "1 lemon", "1 pack asparagus", "1 cup rice"]
    };
}
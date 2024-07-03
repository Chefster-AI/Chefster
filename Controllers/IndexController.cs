using System.Diagnostics;
using System.Security.Claims;
using Chefster.Common;
using Chefster.Models;
using Chefster.Services;
using Chefster.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Chefster.Controllers;

// use this to make swagger ignore this controller if its not really an api
[ApiExplorerSettings(IgnoreApi = true)]
public class IndexController(
    FamilyService familyService,
    MemberService memberService,
    ConsiderationsService considerationsService
) : Controller
{
    private readonly FamilyService _familyService = familyService;
    private readonly MemberService _memberService = memberService;
    private readonly ConsiderationsService _considerationService = considerationsService;

    [Authorize]
    [Route("/chat")]
    public IActionResult Chat()
    {
        return View();
    }

    [Route("/confirm")]
    public IActionResult ConfirmationEmail()
    {
        return View(new { FamilyId = "exampleFamilyId" });
    }

    [Authorize]
    [HttpGet]
    [Route("/createprofile")]
    public IActionResult CreateProfile()
    {
        var id = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value!;
        var family = _familyService.GetById(id).Data;

        if (family == null)
        {
            var model = new FamilyViewModel
            {
                PhoneNumber = "",
                FamilySize = 1,
                NumberOfBreakfastMeals = 0,
                NumberOfLunchMeals = 0,
                NumberOfDinnerMeals = 7,
                GenerationDay = DayOfWeek.Sunday,
                GenerationTime = TimeSpan.Zero,
                TimeZone = "",
                Members = new List<MemberViewModel>
                {
                    new()
                    {
                        Name = "",
                        Restrictions = ConsiderationsLists.RestrictionsList,
                        Goals = ConsiderationsLists.GoalsList,
                        Cuisines = ConsiderationsLists.CuisinesList
                    }
                }
            };

            return View(model);
        }
        else
        {
            return View("Profile");
        }
    }

    [Authorize]
    [HttpGet]
    [Route("/profile")]
    public IActionResult UpdateProfile()
    {
        var id = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var family = _familyService.GetById(id!).Data;
        var viewModelMembers = new List<MemberUpdateViewModel>();

        if (family != null)
        {
            var members = _memberService.GetByFamilyId(family.Id).Data;

            var count = 0;
            foreach (var member in members!)
            {
                var considerations = _considerationService
                    .GetMemberConsiderations(member.MemberId)
                    .Data;
                var goalSelectListsItems = new List<SelectListItem>();
                goalSelectListsItems.AddRange(ConsiderationsLists.GoalsList);
                var restrictionsSelectListsItems = new List<SelectListItem>();
                restrictionsSelectListsItems.AddRange(ConsiderationsLists.RestrictionsList);
                var cuisineSelectListsItems = new List<SelectListItem>();
                cuisineSelectListsItems.AddRange(ConsiderationsLists.CuisinesList);

                if (considerations != null)
                {
                    foreach (var consideration in considerations)
                    {
                        if (consideration.Type == ConsiderationsEnum.Cuisine)
                        {
                            foreach (var item in ConsiderationsLists.CuisinesList)
                            {
                                if (item.Text == consideration.Value)
                                {
                                    cuisineSelectListsItems[count] = new SelectListItem
                                    {
                                        Selected = true,
                                        Text = consideration.Value
                                    };
                                }
                                count += 1;
                            }
                            count = 0;
                        }

                        if (consideration.Type == ConsiderationsEnum.Goal)
                        {
                            foreach (var item in ConsiderationsLists.GoalsList)
                            {
                                if (item.Text == consideration.Value)
                                {
                                    goalSelectListsItems[count] = new SelectListItem
                                    {
                                        Selected = true,
                                        Text = consideration.Value
                                    };
                                }
                                count += 1;
                            }
                            count = 0;
                        }

                        if (consideration.Type == ConsiderationsEnum.Restriction)
                        {
                            foreach (var item in ConsiderationsLists.RestrictionsList)
                            {
                                if (item.Text == consideration.Value)
                                {
                                    restrictionsSelectListsItems[count] = new SelectListItem
                                    {
                                        Selected = true,
                                        Text = consideration.Value
                                    };
                                }
                                count += 1;
                            }
                            count = 0;
                        }
                    }
                    var tooAdd = new MemberUpdateViewModel
                    {
                        MemberId = member.MemberId,
                        Name = member.Name,
                        Notes = member.Notes,
                        Restrictions = restrictionsSelectListsItems,
                        Goals = goalSelectListsItems,
                        Cuisines = cuisineSelectListsItems,
                        ShouldDelete = false
                    };

                    viewModelMembers.Add(tooAdd);
                }
            }

            var populatedModel = new FamilyUpdateViewModel
            {
                PhoneNumber = family.PhoneNumber,
                FamilySize = viewModelMembers.Count,
                GenerationDay = family.GenerationDay,
                GenerationTime = family.GenerationTime,
                TimeZone = family.TimeZone,
                Members = viewModelMembers,
                NumberOfBreakfastMeals = family.NumberOfBreakfastMeals,
                NumberOfLunchMeals = family.NumberOfLunchMeals,
                NumberOfDinnerMeals = family.NumberOfDinnerMeals
            };
            return View(populatedModel);
        }
        else
        {
            // if the family was null we redict to the create profile page
            Console.WriteLine("Family was null");
            return View("CreateProfile");
        }
    }

    [Route("/email")]
    public IActionResult EmailTemplate()
    {
        var model = new GordonResponseModel
        {
            Notes = "Here are the notes regarding the recipes for this week",
            BreakfastRecipes =
            [
                new GordonResponseModel.Recipe
                {
                    DishName = "Bacon and Eggs",
                    PrepareTime = "20 minutes",
                    Servings = 2,
                    Ingredients =
                    [
                        "6 eggs",
                        "6 slices of bacon",
                        "salt",
                        "1 banana",
                        "2 tbsp peanut butter"
                    ],
                    Instructions =
                    [
                        "Scramble the eggs",
                        "Put eggs in the pan to cook",
                        "Fry bacon in the pan of 6 minutes, flip halfway through",
                        "Slice up banana"
                    ]
                }
            ],
            LunchRecipes =
            [
                new GordonResponseModel.Recipe
                {
                    DishName = "Bacon and Eggs",
                    PrepareTime = "20 minutes",
                    Servings = 2,
                    Ingredients =
                    [
                        "6 eggs",
                        "6 slices of bacon",
                        "salt",
                        "1 banana",
                        "2 tbsp peanut butter"
                    ],
                    Instructions =
                    [
                        "Scramble the eggs",
                        "Put eggs in the pan to cook",
                        "Fry bacon in the pan of 6 minutes, flip halfway through",
                        "Slice up banana"
                    ]
                },
                new GordonResponseModel.Recipe
                {
                    DishName = "Bacon and Eggs",
                    PrepareTime = "20 minutes",
                    Servings = 2,
                    Ingredients =
                    [
                        "6 eggs",
                        "6 slices of bacon",
                        "salt",
                        "1 banana",
                        "2 tbsp peanut butter"
                    ],
                    Instructions =
                    [
                        "Scramble the eggs",
                        "Put eggs in the pan to cook",
                        "Fry bacon in the pan of 6 minutes, flip halfway through",
                        "Slice up banana"
                    ]
                }
            ],
            DinnerRecipes =
            [
                new GordonResponseModel.Recipe
                {
                    DishName = "Bacon and Eggs",
                    PrepareTime = "20 minutes",
                    Servings = 2,
                    Ingredients =
                    [
                        "6 eggs",
                        "6 slices of bacon",
                        "salt",
                        "1 banana",
                        "2 tbsp peanut butter"
                    ],
                    Instructions =
                    [
                        "Scramble the eggs",
                        "Put eggs in the pan to cook",
                        "Fry bacon in the pan of 6 minutes, flip halfway through",
                        "Slice up banana"
                    ]
                },
                new GordonResponseModel.Recipe
                {
                    DishName = "Bacon and Eggs",
                    PrepareTime = "20 minutes",
                    Servings = 2,
                    Ingredients =
                    [
                        "6 eggs",
                        "6 slices of bacon",
                        "salt",
                        "1 banana",
                        "2 tbsp peanut butter"
                    ],
                    Instructions =
                    [
                        "Scramble the eggs",
                        "Put eggs in the pan to cook",
                        "Fry bacon in the pan of 6 minutes, flip halfway through",
                        "Slice up banana"
                    ]
                },
                new GordonResponseModel.Recipe
                {
                    DishName = "Bacon and Eggs",
                    PrepareTime = "20 minutes",
                    Servings = 2,
                    Ingredients =
                    [
                        "6 eggs",
                        "6 slices of bacon",
                        "salt",
                        "1 banana",
                        "2 tbsp peanut butter"
                    ],
                    Instructions =
                    [
                        "Scramble the eggs",
                        "Put eggs in the pan to cook",
                        "Fry bacon in the pan of 6 minutes, flip halfway through",
                        "Slice up banana"
                    ]
                }
            ],
            GroceryList =
            [
                "almond milk",
                "chia seeds",
                "maple syrup",
                "banana",
                "rolled oats",
                "blueberries",
                "spinach",
                "avocado",
                "tomato",
                "corn tortillas",
                "black beans",
                "cilantro",
                "lime",
                "green onions",
                "quinoa",
                "cucumber",
                "red bell pepper",
                "olive oil",
                "chicken breast",
                "soy sauce",
                "ginger",
                "garlic",
                "honey",
                "salmon fillet",
                "broccolini",
                "asparagus",
                "lemon",
                "spaghetti squash",
                "marinara sauce",
                "vegan mozzarella",
                "bell peppers",
                "onion",
                "cherry tomatoes",
                "zucchini",
                "mushrooms",
                "taco seasoning",
                "ground beef",
                "cheddar cheese",
                "gluten-free hamburger buns",
                "lettuce",
                "pickles",
                "barbecue sauce",
                "ribeye steak",
                "rosemary",
                "sweet potatoes",
                "green beans"
            ]
        };
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
        );
    }

    public IActionResult Index()
    {
        // for email example
        var model = new GordonResponseModel
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
        return View(model);
    }

    [Route("/privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize]
    [Route("/thankyou")]
    public IActionResult ThankYou(ThankYouViewModel model)
    {
        return View(model);
    }

    [Route("/unsubscribe/{familyId}")]
    public void Unsubscribe(string familyId)
    {
        Console.WriteLine("Unsubscribing family " + familyId);
        // TODO: implement deletion of the family, all the members, all the member considerations, and all the previous recipes for the family
        // TODO: redirect to a "You have unsubscribed" page
    }
}

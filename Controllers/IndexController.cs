using System.Diagnostics;
using System.Security.Claims;
using Chefster.Common;
using Chefster.Models;
using Chefster.Services;
using Chefster.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;

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

            Console.WriteLine(model.ToJson());

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
    public IActionResult Profile()
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
        return View(Common.Constants.GORDON_RESPONSE_EXAMPLE);
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
        return View(Common.Constants.GORDON_RESPONSE_EXAMPLE);
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

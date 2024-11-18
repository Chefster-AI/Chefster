using System.Security.Claims;
using Chefster.Common;
using Chefster.Models;
using Chefster.Services;
using Chefster.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Chefster.Common.Helpers;

namespace Chefster.Controllers;

[Authorize]
[Route("api/family")]
[ApiController]
public class FamilyController(
    AddressService addressService,
    ConsiderationsService considerationsService,
    EmailService emailService,
    FamilyService familyService,
    HubSpotService hubSpotService,
    MemberService memberService,
    JobService jobService,
    ViewToStringService viewToStringService,
    UpdateProfileService updateProfileService,
    LetterQueueService letterQueueService,
    LoggingService loggingService,
    SubscriptionService subscriptionService
) : ControllerBase
{
    private readonly AddressService _addressService = addressService;
    private readonly ConsiderationsService _considerationsService = considerationsService;
    private readonly EmailService _emailService = emailService;
    private readonly FamilyService _familyService = familyService;
    private readonly HubSpotService _hubSpotService = hubSpotService;
    private readonly MemberService _memberService = memberService;
    private readonly JobService _jobService = jobService;
    private readonly ViewToStringService _viewToStringService = viewToStringService;
    private readonly UpdateProfileService _updateProfileService = updateProfileService;
    private readonly LetterQueueService _letterQueueService = letterQueueService;
    private readonly LoggingService _logger = loggingService;
    private readonly SubscriptionService _subscriptionService = subscriptionService;

#if DEBUG
    [HttpGet("{Id}")]
    public ActionResult<FamilyModel> GetFamily(string Id)
    {
        var family = _familyService.GetById(Id);

        if (family == null)
        {
            return NotFound(new { Message = $"No family found with familyId {Id}" });
        }

        return Ok(family.Data);
    }

    [HttpGet]
    public ActionResult<FamilyModel> GetAllFamilies()
    {
        var families = _familyService.GetAll();
        return Ok(families.Data);
    }
#endif

    [HttpPost]
    public async Task<ActionResult> CreateFamily([FromForm] FamilyViewModel Family)
    {
        var familyId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value!;
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!;
        var createdAt = GetUserCurrentTime(Family.TimeZone);
        if (familyId == null || email == null)
        {
            return RedirectToAction("Index", "error", new { route = "/profile" });
        }

        // create the new family
        var NewFamily = new FamilyModel
        {
            Id = familyId,
            Name = Family.Name,
            Email = email,
            UserStatus = UserStatus.NoAccount,
            CreatedAt = createdAt,
            PhoneNumber = Family.PhoneNumber,
            FamilySize = Family.FamilySize,
            NumberOfBreakfastMeals = Family.NumberOfBreakfastMeals,
            NumberOfLunchMeals = Family.NumberOfLunchMeals,
            NumberOfDinnerMeals = Family.NumberOfDinnerMeals,
            GenerationDay = Family.GenerationDay,
            GenerationTime = Family.GenerationTime,
            TimeZone = Family.TimeZone,
        };
        var created = _familyService.CreateFamily(NewFamily);
        if (!created.Success)
        {
            return RedirectToAction("Index", "error", new { route = "/profile" });
        }

        // register as a contact in hub spot
        _hubSpotService.CreateContact(
            created.Data!.Name,
            created.Data.Email,
            created.Data.UserStatus,
            created.Data.PhoneNumber
        );

        // create all members and considerations for family
        var memberSuccess = CreateMembersAndConsiderations(Family);
        if (!memberSuccess.Success)
        {
            return RedirectToAction("Index", "error", new { route = "/profile" });
        }

        // TODO: send email confirming profile creation
        // var body = await _viewToStringService.ViewToStringAsync(
        //     "ConfirmationEmail",
        //     new { FamilyId = NewFamily.Id }
        // );
        // _emailService.SendEmail(NewFamily.Email, "Thanks for signing up for Chefster!", body);

        return RedirectToAction("Account", "Index");
    }

#if DEBUG
    [HttpDelete("{Id}")]
    public ActionResult DeleteFamily(string Id)
    {
        var deleted = _familyService.DeleteFamily(Id);

        if (!deleted.Success)
        {
            return BadRequest($"Error: {deleted.Error}");
        }

        return Ok();
    }

    [HttpPut("{Id}")]
    public ActionResult<FamilyModel> UpdateFamily(string Id, [FromBody] FamilyUpdateDto family)
    {
        var updated = _familyService.UpdateFamily(Id, family);

        if (!updated.Success)
        {
            return BadRequest($"Error: {updated.Error}");
        }

        // once we updated successfully, not now update the job with new generation times
        _jobService.CreateorUpdateEmailJob(updated.Data!.Id);

        return Ok(updated.Data);
    }
#endif

    // this function is specificly for updating through a form since forms only support POST and PUT
    [HttpPost("/api/update/family")]
    public async Task<ActionResult<FamilyModel>> PostUpdateFamily(
        [FromForm] FamilyUpdateViewModel family
    )
    {
        var familyId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (familyId == null)
        {
            return Unauthorized("No Authorized User. Denied");
        }

        // get original before its updated
        var original = _familyService.GetById(familyId).Data!;

        var updatedFamily = new FamilyUpdateDto
        {
            Name = family.Name,
            PhoneNumber = family.PhoneNumber,
            FamilySize = family.FamilySize,
            NumberOfBreakfastMeals = family.NumberOfBreakfastMeals,
            NumberOfLunchMeals = family.NumberOfLunchMeals,
            NumberOfDinnerMeals = family.NumberOfDinnerMeals,
            GenerationDay = family.GenerationDay,
            GenerationTime = family.GenerationTime,
            TimeZone = family.TimeZone,
        };

        var updated = _familyService.UpdateFamily(familyId, updatedFamily);

        if (!updated.Success)
        {
            return RedirectToAction("Index", "error", new { route = "/profile" });
        }

        // once we updated successfully, update the job with new generation times
        _jobService.CreateorUpdateEmailJob(updated.Data!.Id);

        // update hub spot contact
        _hubSpotService.UpdateContact(
            updated.Data.Name,
            updated.Data.Email,
            updated.Data.UserStatus,
            updated.Data.PhoneNumber
        );

        // TODO: Allow user to update subscription status
        // User when from free trial to a letter queue eligible status (extendedFreeTrial or Subscribed)
        // if (
        //     (original.UserStatus == UserStatus.FreeTrial)
        //     && (
        //         updatedFamily.UserStatus == UserStatus.ExtendedFreeTrial
        //         || updatedFamily.UserStatus == UserStatus.Subscribed
        //     )
        // )
        // {
        //     _letterQueueService.PopulateLetterQueue(familyId);
        // }

        // Update old members and create new considerations
        await _updateProfileService.UpdateOrCreateMembersAndCreateConsiderations(familyId, family);

        // probably redirect to summary page
        return RedirectToAction("Index", "profile");
    }

    private ServiceResult<Task> CreateMembersAndConsiderations(FamilyViewModel Family)
    {
        foreach (MemberViewModel Member in Family.Members)
        {
            // create the new member
            var NewMember = new MemberCreateDto
            {
                FamilyId = User
                    .Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                    ?.Value!,
                Name = Member.Name,
                Notes = Member.Notes
            };

            var CreatedMember = _memberService.CreateMember(NewMember);

            if (!CreatedMember.Success)
            {
                return ServiceResult<Task>.ErrorResult(
                    $"Error creating member. Error: {CreatedMember.Error}"
                );
            }

            var allConsiderations = Member
                .Restrictions.Concat(Member.Goals)
                .Concat(Member.Cuisines);

            foreach (SelectListItem c in allConsiderations)
            {
                if (c.Selected)
                {
                    var consideration = new ConsiderationsCreateDto
                    {
                        MemberId = CreatedMember.Data!.MemberId,
                        Type = ConsiderationsEnum.Cuisine, // default
                        Value = c.Text
                    };

                    if (Member.Restrictions.Contains(c))
                    {
                        consideration.Type = ConsiderationsEnum.Restriction;
                    }
                    else if (Member.Goals.Contains(c))
                    {
                        consideration.Type = ConsiderationsEnum.Goal;
                    }
                    else
                    {
                        consideration.Type = ConsiderationsEnum.Cuisine;
                    }

                    var created = _considerationsService.CreateConsideration(consideration);
                    if (!created.Success)
                    {
                        return ServiceResult<Task>.ErrorResult(
                            $"Error creating consideration. Error: {created.Error}"
                        );
                    }
                }
            }
        }
        return ServiceResult<Task>.SuccessResult(Task.CompletedTask);
    }
}

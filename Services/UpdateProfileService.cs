using Chefster.Common;
using Chefster.Models;
using Chefster.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;

namespace Chefster.Services;

public class UpdateProfileService(
    FamilyService familyService,
    MemberService memberService,
    ConsiderationsService considerationsService,
    LoggingService loggingService
)
{
    private readonly FamilyService _familyService = familyService;
    private readonly MemberService _memberService = memberService;
    private readonly ConsiderationsService _considerationsService = considerationsService;
    private readonly LoggingService _logger = loggingService;

    public Task UpdateOrCreateMembersAndCreateConsiderations(
        string familyId,
        FamilyUpdateViewModel Family
    )
    {
        foreach (MemberUpdateViewModel Member in Family.Members)
        {
            MemberModel? contextMember = null;

            // if member exists and we aren't suppose to delete it, update it
            if (Member.MemberId != null && !Member.ShouldDelete)
            {
                var UpdatedMember = new MemberUpdateDto
                {
                    Name = Member.Name,
                    Notes = Member.Notes,
                };

                var updated = _memberService.UpdateMember(Member.MemberId, UpdatedMember);
                if (!updated.Success)
                {
                    _logger.Log(
                        $"Failed to update member {UpdatedMember.ToJson()}. Error: {updated.Error}",
                        LogLevels.Error
                    );
                }
                else
                {
                    contextMember = updated.Data!;
                }
            }

            // if the MemberId is null then it doesnt exist, create it
            if (Member.MemberId == null && !Member.ShouldDelete)
            {
                var NewMember = new MemberCreateDto
                {
                    FamilyId = familyId,
                    Name = Member.Name,
                    Notes = Member.Notes
                };
                var created = _memberService.CreateMember(NewMember);

                if (!created.Success)
                {
                    // We need some way of propgating this to the frontend if it does fail
                    _logger.Log(
                        $"Failed to create member. Member: {NewMember.ToJson()}. Error: {created.Error}",
                        LogLevels.Error
                    );
                }
                else
                {
                    contextMember = created.Data;
                }
            }

            List<string> originalConsiderations = [];
            if (contextMember != null)
            {
                originalConsiderations = _considerationsService
                    .GetMemberConsiderations(contextMember.MemberId)
                    .Data!.Select(c => c.Value)
                    .ToList();
            }

            var allConsiderations = Member
                .Restrictions.Concat(Member.Goals)
                .Concat(Member.Cuisines);

            foreach (SelectListItem c in allConsiderations)
            {
                if (c.Selected && contextMember != null && !originalConsiderations.Contains(c.Text))
                {
                    var consideration = new ConsiderationsCreateDto
                    {
                        MemberId = contextMember.MemberId,
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
                        _logger.Log(
                            $"Error creating consideration. Error: {created.Error}",
                            LogLevels.Error
                        );
                        return Task.FromException(
                            new Exception($"Error creating consideration. Error: {created.Error}")
                        );
                    }
                }
            }

            // if we aren't deleting the member grab a list of remaining considerations
            var remainingConsiderations = !Member.ShouldDelete
                ? Member
                    .Cuisines.Concat(Member.Restrictions)
                    .Concat(Member.Goals)
                    .Where(c => c.Selected)
                    .Select(c => c.Text)
                    .ToList()
                : [];

            if (Member.ShouldDelete && Member.MemberId != null)
            {
                _memberService.DeleteMember(Member.MemberId);
            }

            // delete considerations for deleted and updated members
            _considerationsService.DeleteOldConsiderations(
                Member.MemberId!,
                remainingConsiderations
            );
        }

        // Update Family Size
        var deletedMembers = Family.Members.Where(m => m.ShouldDelete).ToList().Count;
        _familyService.UpdateFamilySize(familyId, Family.Members.Count - deletedMembers);

        return Task.CompletedTask;
    }
}

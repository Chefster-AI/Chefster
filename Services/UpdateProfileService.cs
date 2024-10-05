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

    // Handles the deletion of considerations if a member was to update theirs
    public Task DeleteOldConsiderations(string memberId, DateTime timeAdded)
    {
        var considerations = _considerationsService.GetMemberConsiderations(memberId).Data;

        if (considerations != null)
        {
            foreach (var consideration in considerations)
            {
                if (consideration.CreatedAt <= timeAdded)
                {
                    Console.WriteLine(
                        $"Deleting consideration with Id: {consideration.ConsiderationId}"
                    );
                    var deleted = _considerationsService.DeleteConsideration(
                        consideration.ConsiderationId
                    );

                    if (!deleted.Success)
                    {
                        // Throwing exceptions is not good because we want to continue trying to delete others
                        _logger.Log(
                            $"Failed to delete consideration with Id: {deleted.Data!.ConsiderationId}",
                            LogLevels.Error
                        );
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateOrCreateMembersAndCreateConsiderations(
        string familyId,
        FamilyUpdateViewModel Family
    )
    {
        var memberTotal = 0;
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

            // any considerations made before this time will be deleted
            var deleteAfter = DateTime.UtcNow;

            // Create all new considerations for update
            foreach (SelectListItem r in Member.Restrictions)
            {
                if (r.Selected && contextMember != null)
                {
                    ConsiderationsCreateDto restriction =
                        new()
                        {
                            MemberId = contextMember.MemberId,
                            Type = ConsiderationsEnum.Restriction,
                            Value = r.Text
                        };
                    var created = _considerationsService.CreateConsideration(restriction);
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

            foreach (SelectListItem g in Member.Goals)
            {
                if (g.Selected && contextMember != null)
                {
                    ConsiderationsCreateDto goal =
                        new()
                        {
                            MemberId = contextMember.MemberId,
                            Type = ConsiderationsEnum.Goal,
                            Value = g.Text
                        };
                    var created = _considerationsService.CreateConsideration(goal);
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

            foreach (SelectListItem c in Member.Cuisines)
            {
                if (c.Selected && contextMember != null)
                {
                    ConsiderationsCreateDto cuisine =
                        new()
                        {
                            MemberId = contextMember.MemberId,
                            Type = ConsiderationsEnum.Cuisine,
                            Value = c.Text
                        };
                    var created = _considerationsService.CreateConsideration(cuisine);
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

            if (Member.MemberId != null)
            {
                DeleteOldConsiderations(Member.MemberId, deleteAfter);

                // delete the member if it was checked to delete
                if (Member.ShouldDelete)
                {
                    _memberService.DeleteMember(Member.MemberId);
                    memberTotal += 1;
                    _familyService.UpdateFamilySize(familyId, Family.Members.Count - memberTotal);
                }
            }
        }
        return Task.CompletedTask;
    }
}

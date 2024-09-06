using Chefster.Common;
using Chefster.Context;
using Chefster.Interfaces;
using Chefster.Models;
using Microsoft.Data.SqlClient;
using static Chefster.Common.Constants;

namespace Chefster.Services;

public class MemberService(ChefsterDbContext context) : IMember
{
    private readonly ChefsterDbContext _context = context;

    public ServiceResult<MemberModel> CreateMember(MemberCreateDto member)
    {
        var members = _context.Members.Where(m => m.FamilyId == member.FamilyId).ToList();
        if (members.Count == MAX_MEMBERS)
        {
            return ServiceResult<MemberModel>.ErrorResult(
                $"Member limit reached of {MAX_MEMBERS}."
            );
        }

        var mem = new MemberModel
        {
            MemberId = Guid.NewGuid().ToString("N"), // make a random unique id
            FamilyId = member.FamilyId,
            Name = member.Name,
            Notes = member.Notes
        };

        try
        {
            _context.Members.Add(mem);
            _context.SaveChanges();
            return ServiceResult<MemberModel>.SuccessResult(mem);
        }
        catch (SqlException e)
        {
            return ServiceResult<MemberModel>.ErrorResult($"Failed to create Memeber. Error: {e}");
        }
    }

    public ServiceResult<MemberModel> DeleteMember(string memberId)
    {
        var mem = _context.Members.Find(memberId);
        if (mem == null)
        {
            return ServiceResult<MemberModel>.ErrorResult("Member does not exist");
        }

        try
        {
            _context.Remove(mem);
            _context.SaveChanges();
            return ServiceResult<MemberModel>.SuccessResult(mem);
        }
        catch (SqlException e)
        {
            return ServiceResult<MemberModel>.ErrorResult($"Failed to delete memeber. Error: {e}");
        }
    }

    public ServiceResult<List<MemberModel>> GetByFamilyId(string id)
    {
        try
        {
            var members = _context.Members.Where(e => e.FamilyId == id).ToList();
            return ServiceResult<List<MemberModel>>.SuccessResult(members);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<MemberModel>>.ErrorResult(
                $"Failed to retrieve all Members for family with ID: {id}. Error: {e}"
            );
        }
    }

    public ServiceResult<MemberModel?> GetByMemberId(string id)
    {
        try
        {
            var member = _context.Members.Find(id);
            return ServiceResult<MemberModel?>.SuccessResult(member);
        }
        catch (SqlException e)
        {
            return ServiceResult<MemberModel?>.ErrorResult(
                $"Failed to retrieve Member with ID: {id}. Error: {e}"
            );
        }
    }

    public ServiceResult<MemberModel> UpdateMember(string memberId, MemberUpdateDto member)
    {
        try
        {
            // Find the member
            var existingMem = _context.Members.Find(memberId);
            if (existingMem == null)
            {
                return ServiceResult<MemberModel>.ErrorResult(
                    $"Member does not exist with ID: {memberId}"
                );
            }

            existingMem.Name = member.Name;
            existingMem.Notes = member.Notes;

            _context.SaveChanges();
            return ServiceResult<MemberModel>.SuccessResult(existingMem);
        }
        catch (Exception e)
        {
            return ServiceResult<MemberModel>.ErrorResult($"Failed to update Member. Error: {e}");
        }
    }
}

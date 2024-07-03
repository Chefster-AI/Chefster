using Chefster.Common;
using Chefster.Context;
using Chefster.Interfaces;
using Chefster.Models;
using Microsoft.Data.SqlClient;

namespace Chefster.Services;

public class ConsiderationsService(ChefsterDbContext context) : IConsiderations
{
    private readonly ChefsterDbContext _context = context;

    public ServiceResult<ConsiderationsModel> CreateConsideration(
        ConsiderationsCreateDto consideration
    )
    {
        var n = new ConsiderationsModel
        {
            ConsiderationId = Guid.NewGuid().ToString("N"),
            MemberId = consideration.MemberId,
            Type = consideration.Type,
            Value = consideration.Value,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Considerations.Add(n);
            _context.SaveChanges(); // Save changes to database after altering it
            return ServiceResult<ConsiderationsModel>.SuccessResult(n);
        }
        catch (SqlException e)
        {
            return ServiceResult<ConsiderationsModel>.ErrorResult(
                $"Failed to write note to database. Error: {e}"
            );
        }
    }

    public ServiceResult<ConsiderationsModel> DeleteConsideration(string considerationId)
    {
        try
        {
            var con = _context.Considerations.Find(considerationId);
            if (con == null)
            {
                return ServiceResult<ConsiderationsModel>.ErrorResult(
                    "Consideration doesn't exist"
                );
            }
            _context.Remove(con);
            _context.SaveChanges();
            return ServiceResult<ConsiderationsModel>.SuccessResult(con);
        }
        catch (Exception e)
        {
            return ServiceResult<ConsiderationsModel>.ErrorResult(
                $"Failed to remove consideration from database. Error: {e}"
            );
        }
    }

    public ServiceResult<List<ConsiderationsModel>> GetMemberConsiderations(string memberId)
    {
        try
        {
            var considerations = _context
                .Considerations.Where(c => c.MemberId == memberId)
                .ToList();

            return ServiceResult<List<ConsiderationsModel>>.SuccessResult(considerations);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<ConsiderationsModel>>.ErrorResult(
                $"Failed to get considerations for member with id {memberId}. Error: {e}"
            );
        }
    }

    public ServiceResult<List<ConsiderationsModel>> GetAllFamilyConsiderations(string familyId)
    {
        try
        {
            var mems = _context
                .Members.Where(m => m.FamilyId == familyId)
                .Select(m => m.MemberId)
                .ToList();
            var considerations = _context
                .Considerations.Where(n => mems.Contains(n.MemberId))
                .ToList();
            return ServiceResult<List<ConsiderationsModel>>.SuccessResult(considerations);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<ConsiderationsModel>>.ErrorResult(
                $"Failed to get all notes for family with Id {familyId}. Error: {e}"
            );
        }
    }

    public ServiceResult<List<ConsiderationsModel>> GetWeeklyConsiderations(string familyId)
    {
        var now = DateTime.UtcNow;
        var mems = _context
            .Members.Where(m => m.FamilyId == familyId)
            .Select(m => m.MemberId)
            .ToList();
        // find notes that were made in the last 7 days
        var considerations = _context
            .Considerations.Where(n => mems.Contains(n.MemberId))
            .AsEnumerable() // Load data into memory to use LINQ to Objects
            .Where(n => (now - n.CreatedAt).TotalDays <= 7)
            .ToList();

        return ServiceResult<List<ConsiderationsModel>>.SuccessResult(considerations);
    }

    public ServiceResult<ConsiderationsModel> UpdateConsideration(
        string considerationId,
        ConsiderationsUpdateDto consideration
    )
    {
        try
        {
            var existingConsideration = _context.Considerations.Find(considerationId);
            if (existingConsideration == null)
            {
                return ServiceResult<ConsiderationsModel>.ErrorResult(
                    "consideration does not exist"
                );
            }

            existingConsideration.Type = consideration.Type;
            existingConsideration.Value = consideration.Value;
            _context.SaveChanges();
            return ServiceResult<ConsiderationsModel>.SuccessResult(existingConsideration);
        }
        catch (SqlException e)
        {
            return ServiceResult<ConsiderationsModel>.ErrorResult(
                $"Failed to update consideration with Id {considerationId}. Error: {e}"
            );
        }
    }
}

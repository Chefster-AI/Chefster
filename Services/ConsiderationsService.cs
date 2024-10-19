using Chefster.Common;
using Chefster.Context;
using Chefster.Interfaces;
using Chefster.Models;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;

namespace Chefster.Services;

public class ConsiderationsService(ChefsterDbContext context, LoggingService loggingService)
    : IConsiderations
{
    private readonly ChefsterDbContext _context = context;
    private readonly LoggingService _logger = loggingService;

    public ServiceResult<ConsiderationsModel> CreateConsideration(
        ConsiderationsCreateDto consideration
    )
    {
        var newConsideration = new ConsiderationsModel
        {
            ConsiderationId = Guid.NewGuid().ToString("N"),
            MemberId = consideration.MemberId,
            Type = consideration.Type,
            Value = consideration.Value,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Considerations.Add(newConsideration);
            _context.SaveChanges(); // Save changes to database after altering it
            return ServiceResult<ConsiderationsModel>.SuccessResult(newConsideration);
        }
        catch (SqlException e)
        {
            return ServiceResult<ConsiderationsModel>.ErrorResult(
                $"Failed to insert consideration into database. Error: {e}",
                _logger
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
                    "Consideration doesn't exist",
                    _logger
                );
            }
            _context.Remove(con);
            _context.SaveChanges();
            return ServiceResult<ConsiderationsModel>.SuccessResult(con);
        }
        catch (Exception e)
        {
            return ServiceResult<ConsiderationsModel>.ErrorResult(
                $"Failed to remove consideration from database. Error: {e}",
                _logger
            );
        }
    }

    public ServiceResult<List<ConsiderationsModel>> DeleteOldConsiderations(
        string memberId,
        List<string> remainingConsiderations
    )
    {
        // grab considerations that are for the member and that are not within the remaining list
        var considerations = _context.Considerations.Where(c =>
            memberId == c.MemberId && !remainingConsiderations.Contains(c.Value)
        );

        if (considerations != null)
        {
            _logger.Log(
                $"Deleting considerations: {considerations.Select(c => c.ConsiderationId).ToJson()}",
                LogLevels.Info
            );
            try
            {
                // remove considerations
                _context.RemoveRange(considerations);
                _context.SaveChanges();
                return ServiceResult<List<ConsiderationsModel>>.SuccessResult(
                    considerations.ToList()
                );
            }
            catch (SqlException e)
            {
                return ServiceResult<List<ConsiderationsModel>>.ErrorResult(
                    $"Failed to delete list of considerations. Error: {e}. {considerations.ToJson()}",
                    _logger
                );
            }
        }
        return ServiceResult<List<ConsiderationsModel>>.ErrorResult(
            "considerations was null",
            _logger
        );
    }

    public ServiceResult<ConsiderationsModel> GetConsiderationById(string considerationId)
    {
        try
        {
            var consideration = _context.Considerations.Find(considerationId);

            if (consideration == null)
            {
                return ServiceResult<ConsiderationsModel>.ErrorResult(
                    $"Consideration was null",
                    _logger
                );
            }

            return ServiceResult<ConsiderationsModel>.SuccessResult(consideration);
        }
        catch (SqlException e)
        {
            return ServiceResult<ConsiderationsModel>.ErrorResult(
                $"Failed to find consideration with id {considerationId}. Error: {e}",
                _logger
            );
        }
    }

    public ServiceResult<List<ConsiderationsModel>> GetMemberConsiderations(string memberId)
    {
        try
        {
            // Get considerations that have the same memberId
            var considerations = _context
                .Considerations.Where(c => c.MemberId == memberId)
                .ToList();

            return ServiceResult<List<ConsiderationsModel>>.SuccessResult(considerations);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<ConsiderationsModel>>.ErrorResult(
                $"Failed to get considerations for member with id {memberId}. Error: {e}",
                _logger
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
                $"Failed to get all notes for family with Id {familyId}. Error: {e}",
                _logger
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
                    "consideration does not exist",
                    _logger
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
                $"Failed to update consideration with Id {considerationId}. Error: {e}",
                _logger
            );
        }
    }
}

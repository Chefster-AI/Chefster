using Chefster.Common;
using Chefster.Context;
using Chefster.Interfaces;
using Chefster.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Services;

public class FamilyService(ChefsterDbContext context, LoggingService loggingService) : IFamily
{
    private readonly ChefsterDbContext _context = context;
    private readonly LoggingService _logger = loggingService;

    public ServiceResult<FamilyModel> CreateFamily(FamilyModel family)
    {
        var fam = _context.Families.Find(family.Id);
        if (fam != null)
        {
            _logger.Log($"Family already exists for ID: {family.Id}", LogLevels.Warning);
            return ServiceResult<FamilyModel>.ErrorResult("Family Already Exists");
        }

        try
        {
            _context.Families.Add(family);
            _context.SaveChanges(); // Save changes to database
            _logger.Log($"Created Family with Id: {family.Id}.", LogLevels.Info);
            return ServiceResult<FamilyModel>.SuccessResult(family);
        }
        catch (SqlException e)
        {
            _logger.Log($"Failed to save family. Error {e}", LogLevels.Error);
            return ServiceResult<FamilyModel>.ErrorResult(
                $"Failed to insert Family into database. Error: {e}"
            );
        }
    }

    public ServiceResult<FamilyModel> DeleteFamily(string familyId)
    {
        try
        {
            var fam = _context.Families.Find(familyId);
            if (fam == null)
            {
                return ServiceResult<FamilyModel>.ErrorResult("Family doesn't exist");
            }

            _context.Remove(fam);
            _context.SaveChanges();
            return ServiceResult<FamilyModel>.SuccessResult(fam);
        }
        catch (Exception e)
        {
            return ServiceResult<FamilyModel>.ErrorResult(
                $"Failed to remove family from database with Id {familyId}. Error: {e}"
            );
        }
    }

    // This function is pretty heavy handed if we have a lot of Families. Should only be used for testing
    public ServiceResult<List<FamilyModel>> GetAll()
    {
        try
        {
            return ServiceResult<List<FamilyModel>>.SuccessResult(_context.Families.ToList());
        }
        catch (SqlException e)
        {
            return ServiceResult<List<FamilyModel>>.ErrorResult(
                $"Failed to retrieve all families. Error: {e}"
            );
        }
    }

    public ServiceResult<FamilyModel?> GetById(string familyId)
    {
        try
        {
            var family = _context.Families.Find(familyId);
            return ServiceResult<FamilyModel?>.SuccessResult(family);
        }
        catch (SqlException e)
        {
            return ServiceResult<FamilyModel?>.ErrorResult(
                $"Failed to retrieve all families. Error: {e}"
            );
        }
    }

    public ServiceResult<FamilyModel?> GetByEmail(string email)
    {
        try
        {
            var family = _context.Families.Where(f => f.Email == email).SingleOrDefault();
            return ServiceResult<FamilyModel?>.SuccessResult(family);
        }
        catch (SqlException e)
        {
            return ServiceResult<FamilyModel?>.ErrorResult(
                $"Failed to retrieve family by email: {email}. {e}"
            );
        }
        catch (InvalidOperationException e)
        {
            return ServiceResult<FamilyModel?>.ErrorResult(
                $"Failed to retrieve EXACTLY ONE family by email: {email}. {e}"
            );
        }
    }

    public ServiceResult<List<MemberModel>> GetMembers(string familyId)
    {
        try
        {
            var mem = _context.Members.Where(m => m.FamilyId == familyId).ToList();
            return ServiceResult<List<MemberModel>>.SuccessResult(mem);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<MemberModel>>.ErrorResult(
                $"Failed to retrieve all members for family with id {familyId}. Error: {e}"
            );
        }
    }

    public ServiceResult<FamilyModel> UpdateFamily(string familyId, FamilyUpdateDto family)
    {
        try
        {
            // find the family
            var existingFam = _context.Families.Find(familyId);
            if (existingFam == null)
            {
                _logger.Log($"Family does not exist for update. ID {familyId}", LogLevels.Error);
                return ServiceResult<FamilyModel>.ErrorResult("Family does not exist");
            }

            // update attributes
            existingFam.Name = family.Name;
            existingFam.PhoneNumber = family.PhoneNumber;
            existingFam.FamilySize = family.FamilySize;
            existingFam.GenerationDay = family.GenerationDay;
            existingFam.GenerationTime = family.GenerationTime;
            existingFam.NumberOfBreakfastMeals = family.NumberOfBreakfastMeals;
            existingFam.NumberOfLunchMeals = family.NumberOfLunchMeals;
            existingFam.NumberOfDinnerMeals = family.NumberOfDinnerMeals;
            existingFam.TimeZone = family.TimeZone;

            _context.SaveChanges();

            _logger.Log($"Successfully updated family with familyId: {familyId}", LogLevels.Info);
            // return updated family
            return ServiceResult<FamilyModel>.SuccessResult(existingFam);
        }
        catch (Exception e)
        {
            _logger.Log($"Failed to update Family with Id {familyId}. Error: {e}", LogLevels.Error);
            return ServiceResult<FamilyModel>.ErrorResult(
                $"Failed to update Family with Id {familyId}. Error: {e}"
            );
        }
    }

    // useful utility if we just need to update the size due to the deletion or addition of a member
    public ServiceResult<FamilyModel> UpdateFamilySize(string familyId, int size)
    {
        try
        {
            // find the family
            var existingFam = _context.Families.Find(familyId);
            if (existingFam == null)
            {
                return ServiceResult<FamilyModel>.ErrorResult("Family does not exist");
            }

            existingFam.FamilySize = size;
            _context.SaveChanges();

            return ServiceResult<FamilyModel>.SuccessResult(existingFam);
        }
        catch (Exception e)
        {
            return ServiceResult<FamilyModel>.ErrorResult($"Failed to update Family. Error: {e}");
        }
    }

    public ServiceResult<AddressModel> GetAddressForFamily(string familyId)
    {
        var address = _context.Addresses.Where(a => a.FamilyId == familyId).First();

        if (address == null)
        {
            return ServiceResult<AddressModel>.ErrorResult(
                $"Address was null when querying for familyId: {familyId}"
            );
        }

        return ServiceResult<AddressModel>.SuccessResult(address);
    }

    public ServiceResult<FamilyModel> UpdateFamilyJobTimestamp(string familyId, DateTime timestamp)
    {
        try
        {
            var family = _context.Families.Find(familyId);
            if (family == null)
            {
                return ServiceResult<FamilyModel>.ErrorResult("Family does not exist");
            }

            family.JobTimestamp = timestamp;
            _context.SaveChanges();

            return ServiceResult<FamilyModel>.SuccessResult(family);
        }
        catch (Exception e)
        {
            return ServiceResult<FamilyModel>.ErrorResult(
                $"Failed to update Family Job Timestamp. Error: {e}"
            );
        }
    }

    public ServiceResult<FamilyModel> SetFamilyUserStatus(string familyId, UserStatus userStatus)
    {
        try
        {
            var family = _context.Families.Find(familyId);
            if (family == null)
            {
                return ServiceResult<FamilyModel>.ErrorResult("Family does not exist");
            }

            family.UserStatus = userStatus;
            _context.SaveChanges();

            return ServiceResult<FamilyModel>.SuccessResult(family);
        }
        catch (Exception e)
        {
            return ServiceResult<FamilyModel>.ErrorResult(
                $"Failed to set user status to {userStatus} for family {familyId}. Error: {e}"
            );
        }
    }
}

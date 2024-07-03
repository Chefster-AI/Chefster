using Chefster.Common;
using Chefster.Context;
using Chefster.Interfaces;
using Chefster.Models;
using Microsoft.Data.SqlClient;

namespace Chefster.Services;

public class FamilyService(ChefsterDbContext context) : IFamily
{
    private readonly ChefsterDbContext _context = context;

    public ServiceResult<FamilyModel> CreateFamily(FamilyModel family)
    {
        var fam = _context.Families.Find(family.Id);
        if (fam != null)
        {
            return ServiceResult<FamilyModel>.ErrorResult("Family Already Exists");
        }

        family.CreatedAt = DateTime.UtcNow;

        try
        {
            _context.Families.Add(family);
            _context.SaveChanges(); // Save changes to database after altering it
            return ServiceResult<FamilyModel>.SuccessResult(family);
        }
        catch (SqlException e)
        {
            return ServiceResult<FamilyModel>.ErrorResult(
                $"Failed to write Family to database. Error: {e}"
            );
        }
    }

    public ServiceResult<FamilyModel> DeleteFamily(string id)
    {
        try
        {
            var fam = _context.Families.Find(id);
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
                $"Failed to remove family from database. Error: {e}"
            );
        }
    }

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

    public ServiceResult<FamilyModel?> GetById(string id)
    {
        try
        {
            return ServiceResult<FamilyModel?>.SuccessResult(_context.Families.Find(id));
        }
        catch (SqlException e)
        {
            return ServiceResult<FamilyModel?>.ErrorResult(
                $"Failed to retrieve all families. Error: {e}"
            );
        }
    }

    public ServiceResult<List<MemberModel>> GetMembers(string id)
    {
        try
        {
            var mem = _context.Members.Where(m => m.FamilyId == id).ToList();
            return ServiceResult<List<MemberModel>>.SuccessResult(mem);
        }
        catch (SqlException e)
        {
            return ServiceResult<List<MemberModel>>.ErrorResult(
                $"Failed to retrieve all members for family with id {id}. Error: {e}"
            );
        }
    }

    public ServiceResult<FamilyModel> UpdateFamily(string id, FamilyUpdateDto family)
    {
        try
        {
            // find the family
            var existingFam = _context.Families.Find(id);
            if (existingFam == null)
            {
                return ServiceResult<FamilyModel>.ErrorResult("Family does not exist");
            }

            // update everything even if it wasnt changed. Not the most efficient, but works.
            existingFam.PhoneNumber = family.PhoneNumber;
            existingFam.FamilySize = family.FamilySize;
            existingFam.GenerationDay = family.GenerationDay;
            existingFam.GenerationTime = family.GenerationTime;
            existingFam.NumberOfBreakfastMeals = family.NumberOfBreakfastMeals;
            existingFam.NumberOfLunchMeals = family.NumberOfLunchMeals;
            existingFam.NumberOfDinnerMeals = family.NumberOfDinnerMeals;
            existingFam.TimeZone = family.TimeZone;

            // we've edited the item in context, now just save it
            _context.SaveChanges();
            // return existing family since it should be the same as the new one "family"
            return ServiceResult<FamilyModel>.SuccessResult(existingFam);
        }
        catch (Exception e)
        {
            return ServiceResult<FamilyModel>.ErrorResult($"Failed to update Family. Error: {e}");
        }
    }

    public ServiceResult<FamilyModel> UpdateFamilySize(string id, int size)
    {
        try
        {
            // find the family
            var existingFam = _context.Families.Find(id);
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
}

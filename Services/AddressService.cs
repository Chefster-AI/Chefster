using Chefster.Common;
using Chefster.Context;
using Chefster.Models;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;

namespace Chefster.Services;

public class AddressService(ChefsterDbContext context)
{
    private readonly ChefsterDbContext _context = context;

    public async Task<ServiceResult<AddressModel>> CreateAddress(AddressModel address)
    {
        try
        {
            await _context.Addresses.AddAsync(address);
            await _context.SaveChangesAsync();
            return ServiceResult<AddressModel>.SuccessResult(address);
        }
        catch (SqlException e)
        {
            return ServiceResult<AddressModel>.ErrorResult(
                $"Failed to create address {address.ToJson()}. Error {e}"
            );
        }
    }

    public async Task<ServiceResult<AddressModel?>> GetAddress(string familyId)
    {
        try {
            var address = await _context.Addresses.FindAsync(familyId);
            return ServiceResult<AddressModel?>.SuccessResult(address);
        }
        catch (SqlException e)
        {
            return ServiceResult<AddressModel?>.ErrorResult(
                $"Failed to retrieve Address for FamilyId: {familyId}. Error: {e}"
            );
        }

    }
}

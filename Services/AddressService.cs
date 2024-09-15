using Chefster.Common;
using Chefster.Context;
using Chefster.Models;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;

namespace Chefster.Services;

public class AddressService(ChefsterDbContext context, LoggingService loggingService)
{
    private readonly ChefsterDbContext _context = context;
    private readonly LoggingService _logger = loggingService;

    public ServiceResult<AddressModel> CreateAddress(AddressModel address)
    {
        try
        {
            _context.Addresses.Add(address);
            _context.SaveChanges(); // Save changes to database
            return ServiceResult<AddressModel>.SuccessResult(address);
        }
        catch (SqlException e)
        {
            _logger.Log($"Failed to create address {address.ToJson()}. Error {e}", LogLevels.Error);
            return ServiceResult<AddressModel>.ErrorResult(
                $"Failed to create address {address.ToJson()}. Error {e}"
            );
        }
    }

}

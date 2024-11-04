using Chefster.Common;
using Chefster.Context;
using Chefster.Models;

namespace Chefster.Services;

public class SubscriberService(IConfiguration configuration, ChefsterDbContext context)
{
    private readonly string? privateKey = configuration["STRIPE_TEST_KEY"];
    private readonly string? publishableKey = configuration["PUBLISHABLE_KEY"];
    private readonly ChefsterDbContext _context = context;

    public ServiceResult<AccountModel> CreateSubscriber(AccountModel model)
    {
        try
        {
            _context.Accounts.Add(model);
            _context.SaveChanges();
            return ServiceResult<AccountModel>.SuccessResult(model);
        }
        catch (Exception e)
        {
            return ServiceResult<AccountModel>.ErrorResult(
                $"Failed to save subscriber to Database. Error: {e} Model: {model}"
            );
        }
    }

    public ServiceResult<AccountModel> UpdateSubscriber(string familyId, AccountUpdateDto model)
    {
        try
        {
            var current = _context.Accounts.Find(familyId);

            if (current == null)
            {
                return ServiceResult<AccountModel>.ErrorResult(
                    $"Subsciber does not exist for familyId: {familyId}"
                );
            }

            current.PaymentStatus = model.PaymentStatus;
            current.ReceiptUrl = model.ReceiptUrl;

            _context.SaveChanges();
            return ServiceResult<AccountModel>.SuccessResult(current);
        }
        catch (Exception e)
        {
            return ServiceResult<AccountModel>.ErrorResult(
                $"Failed to update Subscriber for familyId: {familyId}. Error: {e}"
            );
        }
    }
}

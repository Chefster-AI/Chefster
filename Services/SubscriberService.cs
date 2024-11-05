using Chefster.Common;
using Chefster.Context;
using Chefster.Models;

namespace Chefster.Services;

public class SubscriberService(ChefsterDbContext context)
{
    private readonly ChefsterDbContext _context = context;

    public ServiceResult<SubscriberModel> CreateSubscriber(SubscriberModel model)
    {
        try
        {
            _context.Subscribers.Add(model);
            _context.SaveChanges();
            return ServiceResult<SubscriberModel>.SuccessResult(model);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriberModel>.ErrorResult(
                $"Failed to save subscriber to Database. Error: {e} Model: {model}"
            );
        }
    }

    public ServiceResult<SubscriberModel> UpdateSubscriber(
        string familyId,
        SubscriberUpdateDto model
    )
    {
        try
        {
            var current = _context.Subscribers.Find(familyId);

            if (current == null)
            {
                return ServiceResult<SubscriberModel>.ErrorResult(
                    $"Subsciber does not exist for familyId: {familyId}"
                );
            }

            current.PaymentStatus = model.PaymentStatus;
            current.ReceiptUrl = model.ReceiptUrl;

            _context.SaveChanges();
            return ServiceResult<SubscriberModel>.SuccessResult(current);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriberModel>.ErrorResult(
                $"Failed to update Subscriber for familyId: {familyId}. Error: {e}"
            );
        }
    }
}

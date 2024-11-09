using Chefster.Common;
using Chefster.Context;
using Chefster.Models;
using MongoDB.Bson;

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

    public ServiceResult<SubscriberModel?> GetSubscriberByFamilyId(string familyId)
    {
        try
        {
            var subscriber = _context.Subscribers.Find(familyId);
            return ServiceResult<SubscriberModel?>.SuccessResult(subscriber ?? null);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriberModel?>.ErrorResult(
                $"Failed to get Subscriber with familyId: {familyId}. Error: {e}"
            );
        }
    }

    public ServiceResult<SubscriberModel?> GetSubscriberByCustomerId(string customerId)
    {
        try
        {
            var subscriber = _context.Subscribers.Where((s) => s.CustomerId == customerId).First();
            return ServiceResult<SubscriberModel?>.SuccessResult(subscriber);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriberModel?>.ErrorResult(
                $"Failed to get Subscriber with customerId: {customerId}. Error: {e}"
            );
        }
    }

    public ServiceResult<bool> SubscriberExists(string customerId)
    {
        try
        {
            var exists = _context.Subscribers.Any((s) => s.CustomerId == customerId);
            return ServiceResult<bool>.SuccessResult(exists);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.ErrorResult(
                $"Failed to check if a subscriber exists for customerId: {customerId}. Error: {e}"
            );
        }
    }

    public ServiceResult<SubscriberModel> UpdateSubscriberByFamilyId(
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

            current.CustomerId = model.CustomerId ?? current.CustomerId;
            current.SubscriptionId = model.SubscriptionId ?? current.SubscriptionId;
            current.PaymentCreatedDate = model.PaymentCreatedDate ?? current.PaymentCreatedDate;
            current.UserStatus = model.UserStatus ?? current.UserStatus;
            current.ReceiptUrl = model.ReceiptUrl ?? current.ReceiptUrl;

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

    public async Task<ServiceResult<SubscriberModel>> UpdateSubscriberByCustomerId(
        string customerId,
        SubscriberUpdateDto model
    )
    {
        try
        {
            var current = _context.Subscribers.Where((s) => s.CustomerId == customerId).First();

            if (current == null)
            {
                return ServiceResult<SubscriberModel>.ErrorResult(
                    $"Subsciber does not exist for customerId: {customerId}"
                );
            }

            current.CustomerId = model.CustomerId ?? current.CustomerId;
            current.SubscriptionId = model.SubscriptionId ?? current.SubscriptionId;
            current.PaymentCreatedDate = model.PaymentCreatedDate ?? current.PaymentCreatedDate;
            current.UserStatus = model.UserStatus ?? current.UserStatus;
            current.ReceiptUrl = model.ReceiptUrl ?? current.ReceiptUrl;

            await _context.SaveChangesAsync();
            return ServiceResult<SubscriberModel>.SuccessResult(current);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriberModel>.ErrorResult(
                $"Failed to update Subscriber for customerId: {customerId}. Error: {e}"
            );
        }
    }
}

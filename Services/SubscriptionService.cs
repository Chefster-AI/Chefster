using Chefster.Common;
using Chefster.Context;
using Chefster.Models;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Services;

public class SubscriptionService(ChefsterDbContext context)
{
    private readonly ChefsterDbContext _context = context;

    public async Task<ServiceResult<SubscriptionModel>> CreateSubscription(SubscriptionModel model)
    {
        try
        {
            await _context.Subscriptions.AddAsync(model);
            await _context.SaveChangesAsync();
            return ServiceResult<SubscriptionModel>.SuccessResult(model);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriptionModel>.ErrorResult(
                $"Failed to save subscription to Database. Error: {e} Model: {model}"
            );
        }
    }
    public async Task<ServiceResult<SubscriptionModel>> GetSubscriptionById(string subscriptionId)
    {
        try
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            return ServiceResult<SubscriptionModel>.SuccessResult(subscription!);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriptionModel>.ErrorResult(
                $"Failed to get subscription for Id: {subscriptionId}. Error: {e}"
            );
        }
    }
    
    public async Task<ServiceResult<SubscriptionModel>> GetLatestSubscriptionByEmail(string email)
    {
        try
        {
            var latestSubscription = await _context.Subscriptions
                .Where(s => s.Email == email)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();
            return ServiceResult<SubscriptionModel>.SuccessResult(latestSubscription!);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriptionModel>.ErrorResult(
                $"Failed to get latest subscription for email: {email}. Error: {e}"
            );
        }
    }

    public async Task<ServiceResult<SubscriptionModel>> UpdateUserStatus(string subscriptionId, UserStatus userStatus)
    {
        try
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            subscription.UserStatus = userStatus;
            await _context.SaveChangesAsync();
            return ServiceResult<SubscriptionModel>.SuccessResult(subscription);
        }
        catch (Exception e)
        {
            return ServiceResult<SubscriptionModel>.ErrorResult(
                $"Failed to update UserStatus for: {subscriptionId}. Error: {e}"
            );
        }
    }
}

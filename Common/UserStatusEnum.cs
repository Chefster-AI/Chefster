namespace Chefster.Common;

public enum UserStatus
{
    NoAccount = 0,
    FreeTrial = 1,
    FreeTrialEnded = 2,
    Subscribed = 3,
    SubscriptionEnded = 4,
    Paid = 5,
    NotPaid = 6,
    PendingPayment = 7,
    Unknown = 8
}

namespace Chefster.Common;

public enum UserStatus
{
    NoAccount = 0,
    FreeTrial = 1,
    FreeTrialExpired = 2,
    Subscribed = 3,
    PreviouslySubscribed = 4,
    NotPaid = 5,
    PendingPayment = 6,
    Unknown = 7
}

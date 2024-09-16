using static Chefster.Common.Constants;

namespace Chefster.Common;

public static class UserStatusHelpers
{
    public static bool IsExpired(UserStatus userStatus, DateTime signUpDate, DayOfWeek generationDay, TimeSpan generationTime, DateTime? jobTimestamp, string timeZone)
    {
        // get the current time in the user's time zone
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var userCurrentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

        // compare it to their calcualted final job run
        var userFinalJobRunTime = CalculateFinalJobRun(userStatus, signUpDate, generationDay, generationTime, jobTimestamp);
        return userFinalJobRunTime < userCurrentTime;
    }

    public static DateTime? CalculateFinalJobRun(UserStatus userStatus, DateTime signUpDate, DayOfWeek generationDay, TimeSpan generationTime, DateTime? jobTimestamp)
    {
        switch (userStatus)
        {
            case UserStatus.FreeTrial:
                return CalculateFirstJobRun(signUpDate, generationDay, generationTime).AddDays(NUM_DAYS_FREE_TRIAL);
            case UserStatus.ExtendedFreeTrial:
                return CalculateFirstJobRun(signUpDate, generationDay, generationTime).AddDays(NUM_DAYS_EXTENDED_FREE_TRIAL);
            case UserStatus.Subscribed:
                return null;
            case UserStatus.FreeTrialExpired:
                return jobTimestamp;
            case UserStatus.PreviouslySubscribed:
                return jobTimestamp;
            case UserStatus.Unknown:
                return null;
            default:
                return null;
        }
    }

    public static DateTime CalculateFirstJobRun(DateTime signUpDate, DayOfWeek generationDay, TimeSpan generationTime)
    {
        // Calculate the difference in days between the sign up day and the generation day
        int daysUntilGenerationDay = ((int)generationDay - (int)signUpDate.DayOfWeek + 7) % 7;
        
        // If the sign up day and generation day are the same, check if the generation time is still ahead
        if (daysUntilGenerationDay == 0 && signUpDate.TimeOfDay >= generationTime)
        {
            daysUntilGenerationDay = 7; // If it's the same day but the time has passed, jump to next week
        }

        // Calculate the date time of the first job run
        DateTime firstJobRun = signUpDate.AddDays(daysUntilGenerationDay).Add(generationTime);

        return firstJobRun;
    }
}
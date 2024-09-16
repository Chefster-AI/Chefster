using Chefster.Services;

namespace Chefster.Common;

public static class UserStatusHelpers
{
    public static bool IsExpired(UserStatus userStatus, DateTime createdAt, DayOfWeek generationDay, TimeSpan generationTime, DateTime? jobTimestamp, string timeZone)
    {
        // get the current time in the user's time zone
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var userCurrentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

        // compare it to their calcualted last job run
        var userLastJobRunTime = CalculateLastJobRun(userStatus, createdAt, generationDay, generationTime, jobTimestamp);
        return userLastJobRunTime < userCurrentTime;
    }

    public static DateTime? CalculateLastJobRun(UserStatus userStatus, DateTime createdAt, DayOfWeek generationDay, TimeSpan generationTime, DateTime? jobTimestamp)
    {
        switch (userStatus)
        {
            case UserStatus.FreeTrial:
                return CalculateFirstJobRun(createdAt, generationDay, generationTime).AddDays(14);
            case UserStatus.ExtendedFreeTrial:
                return CalculateFirstJobRun(createdAt, generationDay, generationTime).AddDays(28);
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

        // Calculate the first generation day and time
        DateTime firstJobRun = signUpDate.AddDays(daysUntilGenerationDay).Date + generationTime;

        return firstJobRun;
    }
}
using static Chefster.Common.Constants;
using Chefster.Common;
using Chefster.Models;
using Hangfire;
using System.Formats.Tar;

namespace Chefster.Services;

public class UserStatusService(FamilyService familyService, LoggingService _loggingService)
{
    private readonly LoggingService _logger = _loggingService;
    private readonly FamilyService _familyService = familyService;

    // checks if the user's service has expired and assigned appropriate status to family
    public void CheckFamilyUserStatus(FamilyModel family)
    {
        if (IsExpired(
            family.UserStatus,
            family.CreatedAt,
            family.GenerationDay,
            family.GenerationTime,
            family.JobTimestamp,
            family.TimeZone))
        {
            RecurringJob.RemoveIfExists(family.Id);
            switch (family.UserStatus)
            {
                case UserStatus.FreeTrial:
                    _logger.Log($"Changing family user status from FreeTrial to FreeTrialExpired for {family.Id}", LogLevels.Info);
                    _familyService.SetFamilyUserStatus(family.Id, UserStatus.FreeTrialExpired);
                    break;
                case UserStatus.ExtendedFreeTrial:
                    _logger.Log($"Changing family user status from ExtendedFreeTrial to FreeTrialExpired for {family.Id}", LogLevels.Info);
                    _familyService.SetFamilyUserStatus(family.Id, UserStatus.FreeTrialExpired);
                    break;
                case UserStatus.Subscribed:
                    _logger.Log($"Changing family user status from Subscribed to PreviouslySubscribed for {family.Id}", LogLevels.Info);
                    _familyService.SetFamilyUserStatus(family.Id, UserStatus.PreviouslySubscribed);
                    break;
                case UserStatus.Unknown:
                    break;
                default:
                    _logger.Log($"Family {family.Id} didn't have a valid UserStatus after expiration, setting UserStatus to Unknown", LogLevels.Error);
                    _familyService.SetFamilyUserStatus(family.Id, UserStatus.Unknown);
                    break;
            }
        }
    }

    public bool IsExpired(UserStatus userStatus, DateTime signUpDate, DayOfWeek generationDay, TimeSpan generationTime, DateTime? jobTimestamp, string timeZone)
    {
        // get the current time in the user's time zone
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var userCurrentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

        // compare it to their calcualted final job run
        var userFinalJobRunTime = CalculateFinalJobRun(userStatus, signUpDate, generationDay, generationTime, jobTimestamp);
        return userFinalJobRunTime < userCurrentTime;
    }

    public DateTime? CalculateFinalJobRun(UserStatus userStatus, DateTime signUpDate, DayOfWeek generationDay, TimeSpan generationTime, DateTime? jobTimestamp)
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

    public DateTime CalculateFirstJobRun(DateTime signUpDate, DayOfWeek generationDay, TimeSpan generationTime)
    {
        // Calculate the difference in days between the sign up day and the generation day
        int daysUntilGenerationDay = ((int)generationDay - (int)signUpDate.DayOfWeek + 7) % 7;
        
        // If the sign up day and generation day are the same, check if the generation time is still ahead
        if (daysUntilGenerationDay == 0 && signUpDate.TimeOfDay >= generationTime)
        {
            daysUntilGenerationDay = 7; // If it's the same day but the time has passed, jump to next week
        }

        // Calculate the date time of the first job run
        DateTime firstJobRun = signUpDate.AddDays(daysUntilGenerationDay) + generationTime;

        return firstJobRun;
    }
}
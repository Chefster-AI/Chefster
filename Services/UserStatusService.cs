using Chefster.Common;
using Hangfire;
using static Chefster.Common.Constants;

namespace Chefster.Services;

public class UserStatusService(
    FamilyService familyService,
    JobRecordService jobRecordService,
    LoggingService loggingService
)
{
    private readonly FamilyService _familyService = familyService;
    private readonly JobRecordService _jobRecordService = jobRecordService;
    private readonly LoggingService _logger = loggingService;

    // checks if the user's service has expired and assigned appropriate status to family
    public void CheckFamilyUserStatus(string familyId, UserStatus userStatus)
    {
        if (IsExpired(familyId, userStatus))
        {
            RecurringJob.RemoveIfExists(familyId);
            switch (userStatus)
            {
                case UserStatus.FreeTrial:
                    _logger.Log(
                        $"Changing family user status from FreeTrial to FreeTrialExpired for {familyId}",
                        LogLevels.Info
                    );
                    _familyService.SetFamilyUserStatus(familyId, UserStatus.FreeTrialExpired);
                    break;
                case UserStatus.ExtendedFreeTrial:
                    _logger.Log(
                        $"Changing family user status from ExtendedFreeTrial to FreeTrialExpired for {familyId}",
                        LogLevels.Info
                    );
                    _familyService.SetFamilyUserStatus(familyId, UserStatus.FreeTrialExpired);
                    break;
                case UserStatus.Subscribed:
                    _logger.Log(
                        $"Changing family user status from Subscribed to PreviouslySubscribed for {familyId}",
                        LogLevels.Info
                    );
                    _familyService.SetFamilyUserStatus(familyId, UserStatus.PreviouslySubscribed);
                    break;
                case UserStatus.Unknown:
                    break;
                default:
                    _logger.Log(
                        $"Family {familyId} didn't have a valid UserStatus after expiration, setting UserStatus to Unknown",
                        LogLevels.Error
                    );
                    _familyService.SetFamilyUserStatus(familyId, UserStatus.Unknown);
                    break;
            }
        }
    }

    public bool IsExpired(string familyId, UserStatus userStatus)
    {
        var result = _jobRecordService.GetNumberOfJobsRan(familyId);
        if (result.Success)
        {
            var numberOfJobsRan = result.Data;
            switch (userStatus)
            {
                case UserStatus.FreeTrial:
                    return NUMBER_OF_JOBS_IN_FREE_TRIAL <= numberOfJobsRan;
                case UserStatus.ExtendedFreeTrial:
                    return NUMBER_OF_JOBS_IN_EXTENDED_FREE_TRIAL <= numberOfJobsRan;
                case UserStatus.Subscribed:
                    return false;
                case UserStatus.FreeTrialExpired:
                    return true;
                case UserStatus.PreviouslySubscribed:
                    return true;
                default:
                    return true;
            }
        }
        return true;
    }

    public DateTime? CalculateFinalJobRun(
        UserStatus userStatus,
        DateTime signUpDate,
        DayOfWeek generationDay,
        TimeSpan generationTime,
        DateTime? jobTimestamp
    )
    {
        switch (userStatus)
        {
            case UserStatus.FreeTrial:
                return CalculateFirstJobRun(signUpDate, generationDay, generationTime).AddDays(7);
            case UserStatus.ExtendedFreeTrial:
                return CalculateFirstJobRun(signUpDate, generationDay, generationTime).AddDays(21);
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

    public DateTime CalculateFirstJobRun(
        DateTime signUpDate,
        DayOfWeek generationDay,
        TimeSpan generationTime
    )
    {
        DateTime result = signUpDate.Date + generationTime;

        if (result <= signUpDate)
        {
            result = result.AddDays(1);
        }

        int daysUntilGenerationDay = ((int)generationDay - (int)result.DayOfWeek + 7) % 7;
        result = result.AddDays(daysUntilGenerationDay);

        return result;
    }
}

using Chefster.Common;
using Chefster.Context;
using Chefster.Models;
using MongoDB.Bson;

namespace Chefster.Services;

public class JobRecordService(ChefsterDbContext context, LoggingService loggingService)
{
    private readonly ChefsterDbContext _context = context;
    private readonly LoggingService _logginService = loggingService;

    public ServiceResult<JobRecordModel> CreateJobRecord(JobRecordCreateDto jobRecord)
    {
        try
        {
            var newJobRecord = new JobRecordModel
            {
                JobId = Guid.NewGuid().ToString("N"),
                FamilyId = jobRecord.FamilyId,
                StartTime = jobRecord.StartTime,
                EndTime = jobRecord.EndTime,
                Duration = jobRecord.StartTime - jobRecord.EndTime,
                JobStatus = jobRecord.JobStatus,
                JobType = jobRecord.JobType
            };

            var result = _context.JobRecords.Add(newJobRecord);
            
            return ServiceResult<JobRecordModel>.SuccessResult(result.Entity);
        }
        catch (Exception e)
        {
            _logginService.Log($"Failed to create job record: {jobRecord.ToJson()}. Error: {e}", LogLevels.Error);
            return ServiceResult<JobRecordModel>.ErrorResult($"Failed to create job record: {jobRecord.ToJson()}. Error: {e}");
        }
    }

    public ServiceResult<int> GetNumberOfJobsRan(string familyId)
    {
        try
        {
            var numberOfJobsRan = _context.JobRecords.Where(j => j.FamilyId == familyId).Count();
            
            return ServiceResult<int>.SuccessResult(numberOfJobsRan);
        }
        catch (Exception e)
        {
            _logginService.Log($"Failed to get number of jobs for family: {familyId}. Error: {e}", LogLevels.Error);
            return ServiceResult<int>.ErrorResult($"Failed to get number of jobs for family: {familyId}. Error: {e}");
        }
    }
}

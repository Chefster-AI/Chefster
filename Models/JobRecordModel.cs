using Chefster.Common;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[PrimaryKey("JobId")]
public class JobRecordModel
{
    public required string JobId { get; set; }
    public required string FamilyId { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public required TimeSpan Duration { get; set; }
    public required JobStatus JobStatus { get; set; }
    public required JobType JobType { get; set; }
}

public class JobRecordCreateDto
{
    public required string FamilyId { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public required JobStatus JobStatus { get; set; }
    public required JobType JobType { get; set; }
}
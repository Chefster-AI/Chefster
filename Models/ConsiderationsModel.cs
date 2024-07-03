using System.ComponentModel.DataAnnotations.Schema;
using Chefster.Common;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("WeeklyNotes")]
[PrimaryKey(nameof(ConsiderationId))]
public class ConsiderationsModel
{
    public required string ConsiderationId { get; set; }
    [ForeignKey(nameof(MemberId))]
    public required string MemberId { get; set; } // foreign key of family model
    public required ConsiderationsEnum Type { get; set; }
    public required string Value { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class ConsiderationsCreateDto
{
    public required string MemberId { get; set; }
    public required ConsiderationsEnum Type { get; set; }
    public required string Value { get; set; }
}

public class ConsiderationsUpdateDto
{
    public required ConsiderationsEnum Type { get; set; }
    public required string Value { get; set; }
}

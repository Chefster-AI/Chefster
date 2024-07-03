using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("Members")]
[PrimaryKey(nameof(MemberId))]
public class MemberModel
{
    public required string MemberId { get; set; }
    public required string FamilyId { get; set; }
    public required string Name { get; set; }
    public string? Notes { get; set; }
}

public class MemberUpdateDto
{
    public required string Name { get; set; }
    public string? Notes { get; set; }
}

public class MemberCreateDto
{
    public required string FamilyId { get; set; }
    public required string Name { get; set; }
    public string? Notes { get; set; }
}

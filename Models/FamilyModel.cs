using System.ComponentModel.DataAnnotations.Schema;
using Chefster.Common;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("Families")]
[PrimaryKey(nameof(Id))]
public class FamilyModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required UserStatus UserStatus { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string PhoneNumber { get; set; }
    public required int FamilySize { get; set; }
    public required int NumberOfBreakfastMeals { get; set; }
    public required int NumberOfLunchMeals { get; set; }
    public required int NumberOfDinnerMeals { get; set; }
    public required DayOfWeek GenerationDay { get; set; }
    public required TimeSpan GenerationTime { get; set; }
    public required string TimeZone { get; set; }
    public DateTime? JobTimestamp { get; set; }
}

public class FamilyUpdateDto
{
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public required int FamilySize { get; set; }
    public required int NumberOfBreakfastMeals { get; set; }
    public required int NumberOfLunchMeals { get; set; }
    public required int NumberOfDinnerMeals { get; set; }
    public required DayOfWeek GenerationDay { get; set; }
    public required TimeSpan GenerationTime { get; set; }
    public required string TimeZone { get; set; }
}

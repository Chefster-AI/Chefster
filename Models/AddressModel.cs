using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("Addresses")]
[PrimaryKey(nameof(FamilyId))]
public class AddressModel
{
    public required string FamilyId { get; set; }
    public required string StreetAddress { get; set; }
    public string? AptOrUnitNumber { get; set; }
    public required string CityOrTown { get; set; }
    public required string StateProvinceRegion { get; set; }
    public required string PostalCode { get; set; }
    public required string Country { get; set; }
}

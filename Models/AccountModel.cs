using System.ComponentModel.DataAnnotations.Schema;
using Chefster.Common;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("Subscribers")]
[PrimaryKey(nameof(FamilyId))]
public class AccountModel
{
    public required string FamilyId { get; set; }
    public string? CustomerId { get; set; }
    public string? SubscriptionId { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public string? PaymentCreatedDate { get; set; }
    public string? ReceiptUrl { get; set; }
}

public class AccountUpdateDto
{
    public required PaymentStatus PaymentStatus { get; set; }
    public required string ReceiptUrl { get; set; }
}

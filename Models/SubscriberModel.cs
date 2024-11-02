using System.ComponentModel.DataAnnotations.Schema;
using Chefster.Common;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("Subscribers")]
[PrimaryKey(nameof(FamilyId))]
public class SubscriberModel
{
    public required string FamilyId { get; set; }
    public required string CustomerId { get; set; }
    public required string SubscriptionId { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public required string PaymentCreatedDate { get; set; }
    public required string ReceiptUrl { get; set; }
}

public class SubscriberUpdateDto
{
    public required PaymentStatus PaymentStatus { get; set; }
    public required string ReceiptUrl { get; set; }
}

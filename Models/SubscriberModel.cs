using System.ComponentModel.DataAnnotations.Schema;
using Chefster.Common;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("Subscribers")]
[PrimaryKey(nameof(FamilyId))]
public class SubscriberModel
{
    public required string FamilyId { get; set; }
    public string? CustomerId { get; set; } = null;
    public string? SubscriptionId { get; set; } = null;
    public UserStatus? UserStatus { get; set; } = null;
    public string? PaymentCreatedDate { get; set; } = null;
    public string? ReceiptUrl { get; set; } = null;
}

public class SubscriberUpdateDto
{
    public string? CustomerId { get; set; }
    public string? SubscriptionId { get; set; }
    public UserStatus? UserStatus { get; set; }
    public string? PaymentCreatedDate { get; set; }
    public string? ReceiptUrl { get; set; }
}

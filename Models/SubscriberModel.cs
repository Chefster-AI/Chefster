using System.ComponentModel.DataAnnotations.Schema;
using Chefster.Common;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("Subscribers")]
[PrimaryKey(nameof(SubscriptionId))]
public class SubscriberModel
{
    public required string SubscriptionId { get; set; }
    public required string CustomerId { get; set; }
    public required string Email { get; set; }
    public required UserStatus UserStatus { get; set; }
    public required string InvoiceUrl { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
}

// public class SubscriberUpdateDto
// {
//     public string? CustomerId { get; set; }
//     public string? SubscriptionId { get; set; }
//     public UserStatus? UserStatus { get; set; }
//     public string? PaymentCreatedDate { get; set; }
//     public string? ReceiptUrl { get; set; }
// }

using System.ComponentModel.DataAnnotations.Schema;
using Chefster.Common;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Models;

[Table("Subscriptions")]
[PrimaryKey(nameof(SubscriptionId))]
public class SubscriptionModel
{
    public required string SubscriptionId { get; set; }
    public required string CustomerId { get; set; }
    public required string Email { get; set; }
    public required UserStatus UserStatus { get; set; }
    public required string InvoiceUrl { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
}

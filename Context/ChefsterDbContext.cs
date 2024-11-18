using Chefster.Models;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Context;

public class ChefsterDbContext(DbContextOptions<ChefsterDbContext> options) : DbContext(options)
{
    public DbSet<FamilyModel> Families { get; set; }
    public DbSet<MemberModel> Members { get; set; }
    public DbSet<ConsiderationsModel> Considerations { get; set; }
    public DbSet<PreviousRecipeModel> PreviousRecipes { get; set; }
    public DbSet<AddressModel> Addresses { get; set; }
    public DbSet<JobRecordModel> JobRecords { get; set; }
    public DbSet<SubscriptionModel> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder options)
    {
        options.Entity<FamilyModel>().ToTable("Families");
        options.Entity<MemberModel>().ToTable("Members");
        options.Entity<ConsiderationsModel>().ToTable("Considerations");
        options.Entity<PreviousRecipeModel>().ToTable("PreviousRecipes");
        options.Entity<AddressModel>().ToTable("Addresses");
        options.Entity<JobRecordModel>().ToTable("JobRecords");
        options.Entity<SubscriptionModel>().ToTable("Subscriptions");
    }
}

using Chefster.Models;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Context;

public class ChefsterDbContext(DbContextOptions<ChefsterDbContext> options) : DbContext(options)
{
    // Define all of the tables we have
    public DbSet<FamilyModel> Families { get; set; }
    public DbSet<MemberModel> Members { get; set; }
    public DbSet<ConsiderationsModel> Considerations { get; set; }
    public DbSet<PreviousRecipeModel> PreviousRecipes { get; set; }

    protected override void OnModelCreating(ModelBuilder options)
    {
        // define what model goes to what table
        options.Entity<FamilyModel>().ToTable("Families");
        options.Entity<MemberModel>().ToTable("Members");
        options.Entity<ConsiderationsModel>().ToTable("Considerations");
        options.Entity<PreviousRecipeModel>().ToTable("PreviousRecipes");
    }
}

using Chefster.Common;
using Chefster.Context;
using Chefster.Models;
using Chefster.Services;
using Microsoft.EntityFrameworkCore;

namespace Chefster.Tests;

public class DatabaseFixture
{
    public ChefsterDbContext Context { get; private set; }
    public FamilyService FamilyService { get; private set; }
    public MemberService MemberService { get; private set; }
    public ConsiderationsService ConsiderationsService { get; private set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<ChefsterDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDB" + Guid.NewGuid().ToString())
            .Options;

        Context = new ChefsterDbContext(options);

        FamilyService = new FamilyService(Context);
        MemberService = new MemberService(Context);
        ConsiderationsService = new ConsiderationsService(Context);
    }

    // Add items to database for testing
    public void Initialize()
    {
        Context.Families.AddRange(
            new FamilyModel
            {
                Id = "1",
                CreatedAt = DateTime.Now,
                Email = "test@email.com",
                UserStatus = UserStatus.Unknown,
                FamilySize = 2,
                GenerationDay = DayOfWeek.Friday,
                GenerationTime = new TimeSpan(10000),
                PhoneNumber = "0001112222",
                TimeZone = "America/Chicago",
                NumberOfBreakfastMeals = 2,
                NumberOfLunchMeals = 3,
                NumberOfDinnerMeals = 5
            },
            new FamilyModel
            {
                Id = "4",
                CreatedAt = DateTime.Now,
                Email = "test4@email.com",
                UserStatus = UserStatus.Unknown,
                FamilySize = 8,
                GenerationDay = DayOfWeek.Monday,
                GenerationTime = new TimeSpan(19000),
                PhoneNumber = "5556664444",
                TimeZone = "America/Chicago",
                NumberOfBreakfastMeals = 2,
                NumberOfLunchMeals = 3,
                NumberOfDinnerMeals = 5
            }
        );

        Context.Members.AddRange(
            new MemberModel
            {
                MemberId = "mem1",
                FamilyId = "1",
                Name = "testName"
            },
            new MemberModel
            {
                MemberId = "mem2",
                FamilyId = "1",
                Name = "testName2"
            },
            new MemberModel
            {
                MemberId = "mem3",
                FamilyId = "1",
                Name = "testName3"
            },
            new MemberModel
            {
                MemberId = "mem4",
                FamilyId = "4",
                Name = "testName4"
            }
        );

        Context.Considerations.AddRange(
            new ConsiderationsModel
            {
                ConsiderationId = "consider1",
                MemberId = "mem1",
                Type = ConsiderationsEnum.Goal,
                Value = "Lose weight",
                CreatedAt = DateTime.Now.AddDays(-4)
            },
            new ConsiderationsModel
            {
                ConsiderationId = "consider2",
                MemberId = "mem1",
                Type = ConsiderationsEnum.Note,
                Value = "Gain weight",
                CreatedAt = DateTime.Now.AddDays(-8)
            },
            new ConsiderationsModel
            {
                ConsiderationId = "consider3",
                MemberId = "mem4",
                Type = ConsiderationsEnum.Restriction,
                Value = "More energy",
                CreatedAt = DateTime.Now.AddDays(-2)
            }
        );
        Context.SaveChanges();
    }

    // cleanup db after test run. Runs automatically
    public void Cleanup()
    {
        Context.Database.EnsureDeleted();
        foreach (var entity in Context.ChangeTracker.Entries().ToList())
        {
            entity.State = EntityState.Detached;
        }
    }
}

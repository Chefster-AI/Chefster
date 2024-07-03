using Chefster.Context;
using Chefster.Models;
using Chefster.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chefster.Tests;

public class DatabaseFixture : IAsyncLifetime
{
    public ChefsterDbContext Context { get; private set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<ChefsterDbContext>()
            .UseInMemoryDatabase(databaseName: "TEST_family_service_db")
            .Options;

        Context = new ChefsterDbContext(options);
        InitializeAsync();
    }

    // create the in memory DB with one entry
    public Task InitializeAsync()
    {
        Context.Database.EnsureDeleted();
        Context.Families.AddRange(
            new FamilyModel
            {
                Id = "1",
                CreatedAt = DateTime.Now,
                Email = "test@email.com",
                FamilySize = 2,
                NumberOfBreakfastMeals = 0,
                NumberOfLunchMeals = 0,
                NumberOfDinnerMeals = 7,
                GenerationDay = DayOfWeek.Friday,
                GenerationTime = new TimeSpan(10000),
                TimeZone = "America/Chicago",
                PhoneNumber = "0001112222"
            },
            new FamilyModel
            {
                Id = "4",
                CreatedAt = DateTime.Now,
                Email = "test4@email.com",
                FamilySize = 8,
                NumberOfBreakfastMeals = 0,
                NumberOfLunchMeals = 0,
                NumberOfDinnerMeals = 7,
                GenerationDay = DayOfWeek.Monday,
                GenerationTime = new TimeSpan(19000),
                TimeZone = "America/Chicago",
                PhoneNumber = "5556664444"
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
            }
        );
        Context.SaveChanges();

        // detach all entities from context so that we can freely add and delete what we want
        foreach (var entity in Context.ChangeTracker.Entries().ToList())
        {
            entity.State = EntityState.Detached;
        }
        return Task.CompletedTask;
    }

    // cleanup db after test run. Runs automatically
    public Task DisposeAsync()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
        return Task.CompletedTask;
    }
}

public class FamilyServiceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private readonly FamilyService _familyService = new(fixture.Context);

    [Fact]
    public void GetFamily_ReturnsFamily()
    {
        var family = _familyService.GetById("1");

        Assert.NotNull(family.Data);
        Assert.Equal("test@email.com", family.Data.Email);
        Assert.Equal(2, family.Data.FamilySize);
        Assert.Equal(DayOfWeek.Friday, family.Data.GenerationDay);
        Assert.Equal(new TimeSpan(10000), family.Data.GenerationTime);
        Assert.Equal("0001112222", family.Data.PhoneNumber);
    }

    [Fact]
    public void CreateFamily_SavesFamilyToDatabase()
    {
        var familyToAdd = new FamilyModel
        {
            Id = "2",
            CreatedAt = DateTime.Now,
            Email = "test1@email.com",
            FamilySize = 5,
            NumberOfBreakfastMeals = 0,
            NumberOfLunchMeals = 0,
            NumberOfDinnerMeals = 7,
            GenerationDay = DayOfWeek.Sunday,
            GenerationTime = new TimeSpan(1000),
            TimeZone = "America/Chicago",
            PhoneNumber = "1112223333"
        };

        _familyService.CreateFamily(familyToAdd);
        var family = _familyService.GetById("2");

        // family doesnt exist case
        var failed = _familyService.CreateFamily(familyToAdd);
        Assert.False(failed.Success);

        Assert.NotNull(family.Data);
        Assert.Equal("test1@email.com", family.Data.Email);
        Assert.Equal(5, family.Data.FamilySize);
        Assert.Equal(DayOfWeek.Sunday, family.Data.GenerationDay);
        Assert.Equal(new TimeSpan(1000), family.Data.GenerationTime);
        Assert.Equal("1112223333", family.Data.PhoneNumber);
    }

    [Fact]
    public void UpdateFamily_UpdatesFamilyInDatabase()
    {
        var familyToUpdate = new FamilyModel
        {
            Id = "3",
            CreatedAt = DateTime.Now,
            Email = "test3@email.com",
            FamilySize = 4,
            NumberOfBreakfastMeals = 0,
            NumberOfLunchMeals = 0,
            NumberOfDinnerMeals = 7,
            GenerationDay = DayOfWeek.Wednesday,
            GenerationTime = new TimeSpan(1000),
            TimeZone = "America/Chicago",
            PhoneNumber = "9998887777"
        };

        _familyService.CreateFamily(familyToUpdate);

        var updated = new FamilyUpdateDto
        {
            FamilySize = 10,
            GenerationDay = DayOfWeek.Tuesday,
            GenerationTime = new TimeSpan(100000),
            PhoneNumber = "7778889999",
            NumberOfBreakfastMeals = 2,
            NumberOfLunchMeals = 2,
            NumberOfDinnerMeals = 4,
            TimeZone = "merica"
        };

        _familyService.UpdateFamily("3", updated);

        // family doesnt exist case
        var failed = _familyService.UpdateFamily("doesntExist", updated);
        Assert.False(failed.Success);

        var family = _familyService.GetById("3");
        Assert.NotNull(family.Data);
        Assert.Equal("test3@email.com", family.Data.Email);
        Assert.Equal(10, family.Data.FamilySize);
        Assert.Equal(2, family.Data.NumberOfBreakfastMeals);
        Assert.Equal(2, family.Data.NumberOfLunchMeals);
        Assert.Equal(4, family.Data.NumberOfDinnerMeals);
        Assert.Equal(DayOfWeek.Tuesday, family.Data.GenerationDay);
        Assert.Equal(new TimeSpan(100000), family.Data.GenerationTime);
        Assert.Equal("7778889999", family.Data.PhoneNumber);
        Assert.Equal("merica", family.Data.TimeZone);

    }

    [Fact]
    public void GetAllFamilies_AllFamiliesAreReturned()
    {
        var families = _familyService.GetAll();

        Assert.NotNull(families.Data);
        foreach (var family in families.Data)
        {
            Assert.NotNull(family.Id);
            Assert.NotNull(family.Email);
            Assert.NotNull(family.PhoneNumber);
            Assert.True(family.FamilySize > 0);
        }
    }

    [Fact]
    public void GetFamilyMembers_MembersAreReturned()
    {
        var members = _familyService.GetMembers("1");

        Assert.NotNull(members.Data);
        foreach (var member in members.Data)
        {
            Assert.Equal("1", member.FamilyId);
            Assert.NotNull(member.MemberId);
            Assert.NotNull(member.Name);
        }
    }
}

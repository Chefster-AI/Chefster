using Chefster.Common;
using Chefster.Models;
using Xunit;

namespace Chefster.Tests;

public class FamilyServiceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture = fixture;

    [Fact]
    public void GetFamily_ReturnsFamily()
    {
        _fixture.Initialize();
        var family = _fixture.FamilyService.GetById("1");

        Assert.NotNull(family.Data);
        Assert.Equal("test@email.com", family.Data.Email);
        Assert.Equal(2, family.Data.FamilySize);
        Assert.Equal(DayOfWeek.Friday, family.Data.GenerationDay);
        Assert.Equal(new TimeSpan(10000), family.Data.GenerationTime);
        Assert.Equal("0001112222", family.Data.PhoneNumber);

        _fixture.Cleanup();
    }

    [Fact]
    public void CreateFamily_SavesFamilyToDatabase()
    {
        _fixture.Initialize();
        var familyToAdd = new FamilyModel
        {
            Id = "2",
            CreatedAt = DateTime.Now,
            Email = "test1@email.com",
            UserStatus = UserStatus.Unknown,
            FamilySize = 5,
            NumberOfBreakfastMeals = 0,
            NumberOfLunchMeals = 0,
            NumberOfDinnerMeals = 7,
            GenerationDay = DayOfWeek.Sunday,
            GenerationTime = new TimeSpan(1000),
            TimeZone = "America/Chicago",
            PhoneNumber = "1112223333"
        };

        _fixture.FamilyService.CreateFamily(familyToAdd);
        var family = _fixture.FamilyService.GetById("2");

        // family doesnt exist case
        var failed = _fixture.FamilyService.CreateFamily(familyToAdd);
        Assert.False(failed.Success);

        Assert.NotNull(family.Data);
        Assert.Equal("test1@email.com", family.Data.Email);
        Assert.Equal(5, family.Data.FamilySize);
        Assert.Equal(DayOfWeek.Sunday, family.Data.GenerationDay);
        Assert.Equal(new TimeSpan(1000), family.Data.GenerationTime);
        Assert.Equal("1112223333", family.Data.PhoneNumber);
        Assert.Equal(UserStatus.Unknown, family.Data.UserStatus);

        _fixture.Cleanup();
    }

    [Fact]
    public void UpdateFamily_UpdatesFamilyInDatabase()
    {
        _fixture.Initialize();
        var familyToUpdate = new FamilyModel
        {
            Id = "3",
            CreatedAt = DateTime.Now,
            Email = "test3@email.com",
            UserStatus = UserStatus.ExtendedFreeTrial,
            FamilySize = 4,
            NumberOfBreakfastMeals = 0,
            NumberOfLunchMeals = 0,
            NumberOfDinnerMeals = 7,
            GenerationDay = DayOfWeek.Wednesday,
            GenerationTime = new TimeSpan(1000),
            TimeZone = "America/Chicago",
            PhoneNumber = "9998887777"
        };

        _fixture.FamilyService.CreateFamily(familyToUpdate);

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

        _fixture.FamilyService.UpdateFamily("3", updated);

        // family doesnt exist case
        var failed = _fixture.FamilyService.UpdateFamily("doesntExist", updated);
        Assert.False(failed.Success);

        var family = _fixture.FamilyService.GetById("3");
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

        _fixture.Cleanup();
    }

    [Fact]
    public void GetAllFamilies_AllFamiliesAreReturned()
    {
        _fixture.Initialize();
        var families = _fixture.FamilyService.GetAll();

        Assert.NotNull(families.Data);
        foreach (var family in families.Data)
        {
            Assert.NotNull(family.Id);
            Assert.NotNull(family.Email);
            Assert.NotNull(family.PhoneNumber);
            Assert.True(family.FamilySize > 0);
        }

        _fixture.Cleanup();
    }

    [Fact]
    public void GetFamilyMembers_MembersAreReturned()
    {
        _fixture.Initialize();
        var members = _fixture.FamilyService.GetMembers("1");

        Assert.NotNull(members.Data);
        foreach (var member in members.Data)
        {
            Assert.Equal("1", member.FamilyId);
            Assert.NotNull(member.MemberId);
            Assert.NotNull(member.Name);
        }
        _fixture.Cleanup();
    }
}

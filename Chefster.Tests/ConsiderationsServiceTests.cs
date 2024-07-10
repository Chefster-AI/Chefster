using Chefster.Common;
using Chefster.Models;
using Xunit;

namespace Chefster.Tests;

public class ConsiderationsServiceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture = fixture;

    [Fact]
    public void GetWeeklyConsiderations_ReturnsConsiderations()
    {
        _fixture.Initialize();
        var considerations = _fixture.ConsiderationsService.GetWeeklyConsiderations("1");

        Assert.NotNull(considerations.Data);
        Assert.Single(considerations.Data);

        foreach (var consideration in considerations.Data)
        {
            Assert.Equal("Lose weight", consideration.Value);
        }

        _fixture.Cleanup();
    }

    [Fact]
    public void GetFamilyConsiderations_ReturnsFamilyConsiderations()
    {
        _fixture.Initialize();
        var considerations = _fixture.ConsiderationsService.GetAllFamilyConsiderations("1");

        Assert.NotNull(considerations.Data);
        Assert.Equal(2, considerations.Data.Count);

        foreach (var consideration in considerations.Data)
        {
            Assert.True(
                consideration.Value == "Lose weight" || consideration.Value == "Gain weight"
            );
            Assert.True(
                consideration.Type == ConsiderationsEnum.Goal
                    || consideration.Type == ConsiderationsEnum.Note
            );
        }

        _fixture.Cleanup();
    }

    [Fact]
    public void CreateConsideration_ConsiderationAddedToDB()
    {
        _fixture.Initialize();

        var consideration = new ConsiderationsCreateDto
        {
            MemberId = "mem4",
            Type = ConsiderationsEnum.Cuisine,
            Value = "american",
        };

        _fixture.ConsiderationsService.CreateConsideration(consideration);

        var retreived = _fixture.ConsiderationsService.GetAllFamilyConsiderations("4");

        Assert.NotNull(retreived.Data);
        Assert.Equal(2, retreived.Data.Count);

        foreach (var cons in retreived.Data)
        {
            Assert.NotNull(cons.Value);
        }

        _fixture.Cleanup();
    }

    [Fact]
    public void UpdateConsideration_ReturnsModifiedConsideration()
    {
        _fixture.Initialize();

        var consideration = _fixture.ConsiderationsService.GetConsiderationById("consider3");

        Assert.NotNull(consideration.Data);

        var updated = new ConsiderationsUpdateDto
        {
            MemberId = "mem4",
            Type = ConsiderationsEnum.Note,
            Value = "Updated consideration value"
        };

        _fixture.ConsiderationsService.UpdateConsideration("consider3", updated);

        var checkUpdated = _fixture.ConsiderationsService.GetConsiderationById("consider3");

        Assert.NotNull(checkUpdated.Data);

        Assert.Equal(ConsiderationsEnum.Note, checkUpdated.Data.Type);
        Assert.Equal("Updated consideration value", checkUpdated.Data.Value);

        _fixture.Cleanup();
    }
}

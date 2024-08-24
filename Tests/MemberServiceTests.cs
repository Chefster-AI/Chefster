using Chefster.Models;
using Xunit;

namespace Chefster.Tests;

public class MemberServiceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture = fixture;

    [Fact]
    public void GetMemberById_ReturnsMember()
    {
        _fixture.Initialize();
        var member = _fixture.MemberService.GetByMemberId("mem1");

        Assert.NotNull(member.Data);
        Assert.Equal("1", member.Data.FamilyId);
        Assert.Equal("testName", member.Data.Name);

        _fixture.Cleanup();
    }

    [Fact]
    public void GetMemberByFamily_ReturnsMember()
    {
        _fixture.Initialize();
        var members = _fixture.MemberService.GetByFamilyId("1");

        Assert.NotNull(members.Data);

        foreach (var member in members.Data)
        {
            Assert.NotNull(member.FamilyId);
            Assert.NotNull(member.MemberId);
            Assert.NotNull(member.Name);
        }

        Assert.Equal(3, members.Data.Count);

        _fixture.Cleanup();
    }

    [Fact]
    public void CreateMember_MemberAddedToDB()
    {
        _fixture.Initialize();

        var member = new MemberCreateDto { FamilyId = "4", Name = "testCreateName" };

        _fixture.MemberService.CreateMember(member);

        var retreived = _fixture.MemberService.GetByFamilyId("4");

        Assert.NotNull(retreived.Data);

        foreach (var mem in retreived.Data)
        {
            Assert.NotNull(mem.FamilyId);
        }

        _fixture.Cleanup();
    }

    [Fact]
    public void UpdateMember_ReturnsModifiedMember()
    {
        _fixture.Initialize();

        var member = _fixture.MemberService.GetByMemberId("mem1");

        Assert.NotNull(member.Data);

        var updated = new MemberUpdateDto { Name = "testUpdatedName" };

        _fixture.MemberService.UpdateMember(member.Data.MemberId, updated);

        var checkUpdated = _fixture.MemberService.GetByMemberId("mem1");

        Assert.NotNull(checkUpdated.Data);

        Assert.Equal("testUpdatedName", checkUpdated.Data.Name);

        _fixture.Cleanup();
    }
}

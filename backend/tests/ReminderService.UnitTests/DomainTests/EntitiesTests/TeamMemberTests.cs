using FluentAssertions;
using ReminderService.Domain.Entities;
using ReminderService.Domain.Enums;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class TeamMemberTests
{
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly DateTimeOffset JoinedAt =
        new(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);

    [TestCase(TeamRole.Owner)]
    [TestCase(TeamRole.Member)]
    public void Constructor_WithValidArguments_CreatesMember(TeamRole role)
    {
        var member = new TeamMember(TeamId, UserId, role, JoinedAt);

        member.TeamId.Should().Be(TeamId);
        member.UserId.Should().Be(UserId);
        member.Role.Should().Be(role);
        member.JoinedAt.Should().Be(JoinedAt);
    }

    [TestCase(true, TestName = "Constructor_EmptyTeamId_Throws")]
    [TestCase(false, TestName = "Constructor_EmptyUserId_Throws")]
    public void Constructor_WithEmptyId_Throws(bool emptyTeamId)
    {
        var teamId = emptyTeamId ? Guid.Empty : TeamId;
        var userId = emptyTeamId ? UserId : Guid.Empty;
        var parameterName = emptyTeamId ? "teamId" : "userId";

        Action action = () =>
            new TeamMember(teamId, userId, TeamRole.Member, JoinedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [TestCase(-1)]
    [TestCase(999)]
    public void Constructor_WithUnknownRole_Throws(int roleValue)
    {
        Action action = () =>
            new TeamMember(
                TeamId,
                UserId,
                (TeamRole)roleValue,
                JoinedAt);

        action.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("role");
    }

    [TestCase(TeamRole.Member, TeamRole.Owner)]
    [TestCase(TeamRole.Owner, TeamRole.Member)]
    [TestCase(TeamRole.Owner, TeamRole.Owner)]
    [TestCase(TeamRole.Member, TeamRole.Member)]
    public void ChangeRole_WithValidRole_UpdatesOnlyRole(
        TeamRole initialRole,
        TeamRole newRole)
    {
        var member = new TeamMember(
            TeamId, UserId, initialRole, JoinedAt);

        member.ChangeRole(newRole);

        member.Role.Should().Be(newRole);
        member.TeamId.Should().Be(TeamId);
        member.UserId.Should().Be(UserId);
        member.JoinedAt.Should().Be(JoinedAt);
    }

    [TestCase(TeamRole.Owner, -1)]
    [TestCase(TeamRole.Owner, 999)]
    [TestCase(TeamRole.Member, -1)]
    [TestCase(TeamRole.Member, 999)]
    public void ChangeRole_WithUnknownRole_ThrowsAndPreservesRole(
        TeamRole initialRole,
        int roleValue)
    {
        var member = new TeamMember(
            TeamId, UserId, initialRole, JoinedAt);

        var action = () =>
            member.ChangeRole((TeamRole)roleValue);

        action.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("role");

        member.Role.Should().Be(initialRole);
    }
}
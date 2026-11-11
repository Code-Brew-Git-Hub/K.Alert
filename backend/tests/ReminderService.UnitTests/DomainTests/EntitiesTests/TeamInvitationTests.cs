using FluentAssertions;
using ReminderService.Domain.Entities;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class TeamInvitationTests
{
    private static readonly DateTime CreatedAt =
        new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ExpiresAt = CreatedAt.AddDays(1);

    [Test]
    public void Constructor_ValidArguments_CreatesUnusedInvitation()
    {
        var id = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        const string tokenHash = "token-hash";

        var invitation = new TeamInvitation(
            id,
            teamId,
            tokenHash,
            createdByUserId,
            CreatedAt,
            ExpiresAt);

        invitation.Id.Should().Be(id);
        invitation.TeamId.Should().Be(teamId);
        invitation.TokenHash.Should().Be(tokenHash);
        invitation.CreatedByUserId.Should().Be(createdByUserId);
        invitation.CreatedAt.Should().Be(CreatedAt);
        invitation.ExpiresAt.Should().Be(ExpiresAt);

        invitation.AcceptedByUserId.Should().BeNull();
        invitation.AcceptedAt.Should().BeNull();
        invitation.RevokedAt.Should().BeNull();
    }

    [TestCase("id", TestName = "Constructor_EmptyId_Throws")]
    [TestCase("teamId", TestName = "Constructor_EmptyTeamId_Throws")]
    [TestCase("createdByUserId", TestName = "Constructor_EmptyAuthorId_Throws")]
    public void Constructor_EmptyIdentifier_Throws(string parameterName)
    {
        var action = () => new TeamInvitation(
            parameterName == "id" ? Guid.Empty : Guid.NewGuid(),
            parameterName == "teamId" ? Guid.Empty : Guid.NewGuid(),
            "token-hash",
            parameterName == "createdByUserId" ? Guid.Empty : Guid.NewGuid(),
            CreatedAt,
            ExpiresAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [TestCase(null, TestName = "Constructor_NullTokenHash_Throws")]
    [TestCase("", TestName = "Constructor_EmptyTokenHash_Throws")]
    [TestCase("   ", TestName = "Constructor_WhitespaceTokenHash_Throws")]
    public void Constructor_InvalidTokenHash_Throws(string? tokenHash)
    {
        var action = () => new TeamInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            tokenHash!,
            Guid.NewGuid(),
            CreatedAt,
            ExpiresAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("tokenHash");
    }

    [Test]
    public void Constructor_TokenHashWithSpaces_PreservesOriginalValue()
    {
        const string tokenHash = " token-hash ";

        var invitation = new TeamInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            tokenHash,
            Guid.NewGuid(),
            CreatedAt,
            ExpiresAt);

        invitation.TokenHash.Should().Be(tokenHash);
    }

    [TestCase("createdAt", DateTimeKind.Local)]
    [TestCase("createdAt", DateTimeKind.Unspecified)]
    [TestCase("expiresAt", DateTimeKind.Local)]
    [TestCase("expiresAt", DateTimeKind.Unspecified)]
    public void Constructor_NonUtcDate_Throws(
        string parameterName,
        DateTimeKind kind)
    {
        var createdAt = parameterName == "createdAt"
            ? DateTime.SpecifyKind(CreatedAt, kind)
            : CreatedAt;

        var expiresAt = parameterName == "expiresAt"
            ? DateTime.SpecifyKind(ExpiresAt, kind)
            : ExpiresAt;

        var action = () => new TeamInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            Guid.NewGuid(),
            createdAt,
            expiresAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [TestCase(-1, TestName = "Constructor_ExpirationBeforeCreation_Throws")]
    [TestCase(0, TestName = "Constructor_ExpirationEqualsCreation_Throws")]
    public void Constructor_ExpirationNotAfterCreation_Throws(int offsetSeconds)
    {
        var action = () => new TeamInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            Guid.NewGuid(),
            CreatedAt,
            CreatedAt.AddSeconds(offsetSeconds));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("expiresAt");
    }

    [TestCase(0, TestName = "Accept_AtCreationTime_Succeeds")]
    [TestCase(1, TestName = "Accept_AfterCreationTime_Succeeds")]
    public void Accept_ValidArguments_SetsUserAndDate(int offsetHours)
    {
        var invitation = CreateInvitation();
        var userId = Guid.NewGuid();
        var acceptedAt = CreatedAt.AddHours(offsetHours);

        invitation.Accept(userId, acceptedAt);

        invitation.AcceptedByUserId.Should().Be(userId);
        invitation.AcceptedAt.Should().Be(acceptedAt);
        invitation.RevokedAt.Should().BeNull();
    }

    [Test]
    public void Accept_EmptyUserId_ThrowsAndPreservesState()
    {
        var invitation = CreateInvitation();
        var before = Snapshot(invitation);

        var action = () =>
            invitation.Accept(Guid.Empty, CreatedAt.AddHours(1));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("userId");

        Snapshot(invitation).Should().Be(before);
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Accept_NonUtcDate_ThrowsAndPreservesState(DateTimeKind kind)
    {
        var invitation = CreateInvitation();
        var before = Snapshot(invitation);
        var acceptedAt = DateTime.SpecifyKind(CreatedAt.AddHours(1), kind);

        var action = () => invitation.Accept(Guid.NewGuid(), acceptedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("acceptedAt");

        Snapshot(invitation).Should().Be(before);
    }

    [Test]
    public void Accept_DateBeforeCreation_ThrowsAndPreservesState()
    {
        var invitation = CreateInvitation();
        var before = Snapshot(invitation);

        var action = () =>
            invitation.Accept(Guid.NewGuid(), CreatedAt.AddTicks(-1));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("acceptedAt");

        Snapshot(invitation).Should().Be(before);
    }

    [Test]
    public void Accept_ImmediatelyBeforeExpiration_Succeeds()
    {
        var invitation = CreateInvitation();
        var userId = Guid.NewGuid();
        var acceptedAt = ExpiresAt.AddTicks(-1);

        invitation.Accept(userId, acceptedAt);

        invitation.AcceptedByUserId.Should().Be(userId);
        invitation.AcceptedAt.Should().Be(acceptedAt);
    }

    [TestCase(0, TestName = "Accept_AtExpiration_Throws")]
    [TestCase(1, TestName = "Accept_AfterExpiration_Throws")]
    public void Accept_ExpiredInvitation_ThrowsAndPreservesState(
        int offsetSeconds)
    {
        var invitation = CreateInvitation();
        var before = Snapshot(invitation);

        var action = () => invitation.Accept(
            Guid.NewGuid(),
            ExpiresAt.AddSeconds(offsetSeconds));

        action.Should().Throw<InvalidOperationException>();

        Snapshot(invitation).Should().Be(before);
    }

    [Test]
    public void Accept_AlreadyAccepted_ThrowsAndPreservesOriginalAcceptance()
    {
        var invitation = CreateInvitation();
        invitation.Accept(Guid.NewGuid(), CreatedAt.AddHours(1));
        var before = Snapshot(invitation);

        var action = () =>
            invitation.Accept(Guid.NewGuid(), CreatedAt.AddHours(2));

        action.Should().Throw<InvalidOperationException>();

        Snapshot(invitation).Should().Be(before);
    }

    [Test]
    public void Accept_RevokedInvitation_ThrowsAndPreservesState()
    {
        var invitation = CreateInvitation();
        invitation.Revoke(CreatedAt.AddHours(1));
        var before = Snapshot(invitation);

        var action = () =>
            invitation.Accept(Guid.NewGuid(), CreatedAt.AddHours(2));

        action.Should().Throw<InvalidOperationException>();

        Snapshot(invitation).Should().Be(before);
    }

    [TestCase(0, TestName = "Revoke_AtCreationTime_Succeeds")]
    [TestCase(1, TestName = "Revoke_BeforeExpiration_Succeeds")]
    [TestCase(24, TestName = "Revoke_AtExpiration_Succeeds")]
    [TestCase(25, TestName = "Revoke_AfterExpiration_Succeeds")]
    public void Revoke_UnusedInvitation_SetsRevocationDate(int offsetHours)
    {
        var invitation = CreateInvitation();
        var revokedAt = CreatedAt.AddHours(offsetHours);

        invitation.Revoke(revokedAt);

        invitation.RevokedAt.Should().Be(revokedAt);
        invitation.AcceptedByUserId.Should().BeNull();
        invitation.AcceptedAt.Should().BeNull();
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Revoke_NonUtcDate_ThrowsAndPreservesState(DateTimeKind kind)
    {
        var invitation = CreateInvitation();
        var before = Snapshot(invitation);
        var revokedAt = DateTime.SpecifyKind(CreatedAt.AddHours(1), kind);

        var action = () => invitation.Revoke(revokedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("revokedAt");

        Snapshot(invitation).Should().Be(before);
    }

    [Test]
    public void Revoke_DateBeforeCreation_ThrowsAndPreservesState()
    {
        var invitation = CreateInvitation();
        var before = Snapshot(invitation);

        var action = () => invitation.Revoke(CreatedAt.AddTicks(-1));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("revokedAt");

        Snapshot(invitation).Should().Be(before);
    }

    [Test]
    public void Revoke_AlreadyRevoked_PreservesOriginalRevocation()
    {
        var invitation = CreateInvitation();
        invitation.Revoke(CreatedAt.AddHours(1));
        var before = Snapshot(invitation);

        invitation.Revoke(CreatedAt.AddHours(2));

        Snapshot(invitation).Should().Be(before);
    }

    [Test]
    public void Revoke_AcceptedInvitation_ThrowsAndPreservesState()
    {
        var invitation = CreateInvitation();
        invitation.Accept(Guid.NewGuid(), CreatedAt.AddHours(1));
        var before = Snapshot(invitation);

        var action = () => invitation.Revoke(CreatedAt.AddHours(2));

        action.Should().Throw<InvalidOperationException>();

        Snapshot(invitation).Should().Be(before);
    }

    private static TeamInvitation CreateInvitation()
    {
        return new TeamInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            Guid.NewGuid(),
            CreatedAt,
            ExpiresAt);
    }

    private static InvitationState Snapshot(TeamInvitation invitation)
    {
        return new InvitationState(
            invitation.AcceptedByUserId,
            invitation.AcceptedAt,
            invitation.RevokedAt);
    }

    private sealed record InvitationState(
        Guid? AcceptedByUserId,
        DateTimeOffset? AcceptedAt,
        DateTimeOffset? RevokedAt);
}
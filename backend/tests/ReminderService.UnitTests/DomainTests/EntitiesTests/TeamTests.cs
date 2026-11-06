using FluentAssertions;
using ReminderService.Domain.Entities;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class TeamTests
{
    private static readonly Guid TeamId = Guid.NewGuid();
    private const string Name = "Backend-1";

    private static readonly DateTime CreatedAt =
        new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Constructor_WithValidArguments_CreatesTeam()
    {
        var team = new Team(TeamId, Name, CreatedAt);

        team.Id.Should().Be(TeamId);
        team.Name.Should().Be(Name);
        team.CreatedAt.Should().Be(CreatedAt);
        team.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void Constructor_WithEmptyId_Throws()
    {
        Action action = () =>
            new Team(Guid.Empty, Name, CreatedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("id");
    }

    [TestCase(null, TestName = "Constructor_WithNullName_Throws")]
    [TestCase("", TestName = "Constructor_WithEmptyName_Throws")]
    [TestCase(" ", TestName = "Constructor_WithWhitespaceName_Throws")]
    [TestCase("\t\n", TestName = "Constructor_WithWhitespaceCharactersName_Throws")]
    public void Constructor_WithInvalidName_Throws(string? name)
    {
        Action action = () =>
            new Team(TeamId, name!, CreatedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Constructor_WithNonUtcCreatedAt_Throws(DateTimeKind kind)
    {
        var createdAt = DateTime.SpecifyKind(CreatedAt, kind);

        Action action = () =>
            new Team(TeamId, Name, createdAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("createdAt");
    }

    [TestCase("Backend-1", "Backend-1")]
    [TestCase("Backend-1 ", "Backend-1")]
    [TestCase("Backend-1  ", "Backend-1")]
    [TestCase(" Backend-1", "Backend-1")]
    [TestCase("  Backend-1", "Backend-1")]
    [TestCase(" Backend-1 ", "Backend-1")]
    [TestCase("\tBackend-1\n", "Backend-1")]
    [TestCase(" Backend 1 ", "Backend 1")]
    public void Constructor_WithValidName_TrimsEdges(
        string name,
        string expectedName)
    {
        var team = new Team(TeamId, name, CreatedAt);

        team.Name.Should().Be(expectedName);
    }

    [TestCase(null, TestName = "Rename_WithNullName_ThrowsAndPreservesName")]
    [TestCase("", TestName = "Rename_WithEmptyName_ThrowsAndPreservesName")]
    [TestCase(" ", TestName = "Rename_WithWhitespaceName_ThrowsAndPreservesName")]
    [TestCase("\t\n", TestName = "Rename_WithWhitespaceCharactersName_ThrowsAndPreservesName")]
    public void Rename_WithInvalidName_ThrowsAndPreservesName(
        string? newName)
    {
        var team = new Team(TeamId, Name, CreatedAt);

        var action = () =>
            team.Rename(newName!);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("name");

        team.Name.Should().Be(Name);
    }

    [TestCase("Backend-2", "Backend-2")]
    [TestCase(" Backend-2 ", "Backend-2")]
    [TestCase("  Backend-2", "Backend-2")]
    [TestCase("Backend-2  ", "Backend-2")]
    [TestCase("\tBackend-2\n", "Backend-2")]
    [TestCase(" Backend 2 ", "Backend 2")]
    [TestCase(Name, Name)]
    public void Rename_WithValidName_UpdatesOnlyName(
        string newName,
        string expectedName)
    {
        var team = new Team(TeamId, Name, CreatedAt);

        team.Rename(newName);

        team.Name.Should().Be(expectedName);
        team.Id.Should().Be(TeamId);
        team.CreatedAt.Should().Be(CreatedAt);
    }
}
using FluentAssertions;
using ReminderService.Domain.Entities;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class UserTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private const string DisplayName = "Oleg";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Constructor_WithValidArguments_CreatesUser()
    {
        var user = new User(UserId, DisplayName, CreatedAt);

        user.Id.Should().Be(UserId);
        user.DisplayName.Should().Be(DisplayName);
        user.CreatedAt.Should().Be(CreatedAt);
    }

    [Test]
    public void Constructor_WithEmptyId_Throws()
    {
        Action action = () =>
            new User(Guid.Empty, DisplayName, CreatedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("id");
    }

    [TestCase(null, TestName = "Constructor_WithNullDisplayName_Throws")]
    [TestCase("", TestName = "Constructor_WithEmptyDisplayName_Throws")]
    [TestCase(" ", TestName = "Constructor_WithWhitespaceDisplayName_Throws")]
    [TestCase("\t\n", TestName = "Constructor_WithWhitespaceCharactersDisplayName_Throws")]
    public void Constructor_WithInvalidDisplayName_Throws(string? displayName)
    {
        Action action = () =>
            new User(UserId, displayName!, CreatedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("displayName");
    }

    [TestCase("Oleg", "Oleg")]
    [TestCase("Oleg ", "Oleg")]
    [TestCase("Oleg  ", "Oleg")]
    [TestCase(" Oleg", "Oleg")]
    [TestCase("  Oleg", "Oleg")]
    [TestCase(" Oleg ", "Oleg")]
    [TestCase("\tOleg\n", "Oleg")]
    [TestCase(" Oleg Petrov ", "Oleg Petrov")]
    public void Constructor_WithValidDisplayName_TrimsEdges(
        string displayName,
        string expectedDisplayName)
    {
        var user = new User(UserId, displayName, CreatedAt);

        user.DisplayName.Should().Be(expectedDisplayName);
    }

    [TestCase(null, TestName = "Rename_WithNullDisplayName_ThrowsAndPreservesName")]
    [TestCase("", TestName = "Rename_WithEmptyDisplayName_ThrowsAndPreservesName")]
    [TestCase(" ", TestName = "Rename_WithWhitespaceDisplayName_ThrowsAndPreservesName")]
    [TestCase("\t\n", TestName = "Rename_WithWhitespaceCharactersDisplayName_ThrowsAndPreservesName")]
    public void Rename_WithInvalidDisplayName_ThrowsAndPreservesName(
        string? newDisplayName)
    {
        var user = new User(UserId, DisplayName, CreatedAt);

        var action = () =>
            user.Rename(newDisplayName!);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("displayName");

        user.DisplayName.Should().Be(DisplayName);
    }

    [TestCase("Ivan", "Ivan")]
    [TestCase(" Ivan ", "Ivan")]
    [TestCase("  Ivan", "Ivan")]
    [TestCase("Ivan  ", "Ivan")]
    [TestCase("\tIvan\n", "Ivan")]
    [TestCase(" Ivan Petrov ", "Ivan Petrov")]
    [TestCase(DisplayName, DisplayName)]
    public void Rename_WithValidDisplayName_UpdatesOnlyName(
        string newDisplayName,
        string expectedDisplayName)
    {
        var user = new User(UserId, DisplayName, CreatedAt);

        user.Rename(newDisplayName);

        user.DisplayName.Should().Be(expectedDisplayName);
        user.Id.Should().Be(UserId);
        user.CreatedAt.Should().Be(CreatedAt);
    }
}

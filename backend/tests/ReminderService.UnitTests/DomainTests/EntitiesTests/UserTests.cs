using FluentAssertions;
using ReminderService.Domain.Entities;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class UserTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private const string DisplayName = "Oleg";
    private static readonly DateTime JoinedAt =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var id = Guid.Empty;
        
        var action = () => new User(id, DisplayName, JoinedAt);
        
        action.Should()
            .Throw<ArgumentException>("Пустой идентификатор пользователя должен выкидывать ошибку")
            .WithParameterName("id");
    }

    [TestCase("", TestName = "Constructor_EmptyStringName_ThrowsArgumentException")]
    [TestCase(null, TestName = "Constructor_NullStringName_ThrowsArgumentException")]
    [TestCase(" ", TestName = "Constructor_WhitespaceName_ThrowsArgumentException")]
    [TestCase("\t\n", TestName = "Constructor_WhitespaceCharactersName_ThrowsArgumentException")]
    public void Constructor_InvalidDisplayName_ThrowsArgumentException(string? displayName)
    {
        var action = () => new User(UserId, displayName!, JoinedAt);
        
        action.Should()
            .Throw<ArgumentException>("Пустое имя пользователя должно выкидывать ошибку")
            .WithParameterName("displayName");
    }

    [TestCase(DateTimeKind.Unspecified)]
    [TestCase(DateTimeKind.Local)]
    public void Constructor_DateNotUtc_ThrowsArgumentException(DateTimeKind kind)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, kind);
        
        var action = () => new User(UserId, DisplayName, createdAt);
        
        action.Should()
            .Throw<ArgumentException>("Дата не в формате UTC должна выкидывать ошибку")
            .WithParameterName("createdAt");
    }

    [TestCase("Oleg ", "Oleg",  TestName = "DisplayNameWithWhiteSpaceInRight_ReturnsWithoutWhiteSpace")]
    [TestCase("Oleg  ", "Oleg", TestName = "DisplayNameWithTwoWhiteSpaceInRight_ReturnsWithoutWhiteSpace")]
    [TestCase(" Oleg", "Oleg", TestName = "DisplayNameWithWhiteSpaceInLeft_ReturnsWithoutWhiteSpace")]
    [TestCase("  Oleg", "Oleg", TestName = "DisplayNameWithTwoWhiteSpaceInLeft_ReturnsWithoutWhiteSpace")]
    [TestCase(" Oleg ", "Oleg", TestName = "DisplayNameWithWhiteSpaceInRightAndLeft_ReturnsWithoutWhiteSpace")]
    public void Constructor_ShouldTrimDisplayName(string displayName, string expectedDisplayName)
    {
        var user = new User(UserId, displayName, JoinedAt);
        
        user.DisplayName.Should()
            .Be(expectedDisplayName, "Конструктор должен обрезать пробелы с начала и конца имени пользователя");
    }
    
    [Test]
    public void Constructor_ValidArguments_CreatesUser()
    {
        var user = new User(UserId, DisplayName, JoinedAt);
        
        user.Id.Should().Be(UserId, $"Id пользователя должен совпадать с заданным: {UserId}");
        user.DisplayName.Should().Be(DisplayName, $"DisplayName пользователя должно совпадать с заданным: {DisplayName}");
        user.CreatedAt.Should().Be(JoinedAt, $"CreatedAt пользователя должен совпадать с заданным: {JoinedAt}");
    }
    
    [TestCase("", TestName = "Rename_EmptyString_ThrowsArgumentException")]
    [TestCase(null, TestName = "Rename_NullString_ThrowsArgumentException")]
    [TestCase(" ", TestName = "Rename_WhitespaceName_ThrowsArgumentException")]
    [TestCase("\t\n", TestName = "Rename_WhitespaceCharactersName_ThrowsArgumentException")]
    public void Rename_InvalidDisplayName_ThrowsArgumentException(string? newDisplayName)
    {
        var user = new User(UserId, DisplayName, JoinedAt);
        
        var action = () => user.Rename(newDisplayName!);
        
        action.Should()
            .Throw<ArgumentException>("Невалидное имя при переименовании должно выкидывать ошибку")
            .WithParameterName("displayName");
    }

    [TestCase("", TestName = "Rename_EmptyString_DontChangeOldName")]
    [TestCase(null, TestName = "Rename_NullString_DontChangeOldName")]
    [TestCase(" ", TestName = "Rename_WhitespaceName_DontChangeOldName")]
    [TestCase("\t\n", TestName = "Rename_WhitespaceCharactersName_DontChangeOldName")]
    public void Rename_InvalidDisplayName_DontChangeOldName(string? newDisplayName)
    {
        var user = new User(UserId, DisplayName, JoinedAt);
        
        var act = () => user.Rename(newDisplayName!);

        act.Should().Throw<ArgumentException>();
        user.DisplayName.Should().Be(DisplayName, "Неудачное переименование не должно менять старое имя");
    }
    
    [TestCase("Ivan", "Ivan")]
    [TestCase(" Ivan ", "Ivan")]
    [TestCase("  Ivan", "Ivan")]
    [TestCase("Ivan  ", "Ivan")]
    [TestCase("Ivan Petrov", "Ivan Petrov")]
    public void Rename_ValidDisplayName_UpdatesName(string newDisplayName, string expectedDisplayName)
    {
        var user = new User(UserId, DisplayName, JoinedAt);

        user.Rename(newDisplayName);

        user.DisplayName.Should().Be(expectedDisplayName);
        user.Id.Should().Be(UserId);
        user.CreatedAt.Should().Be(JoinedAt);
    }
}
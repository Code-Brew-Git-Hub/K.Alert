using FluentAssertions;
using ReminderService.Domain.Entities;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class TeamTests
{
    private Guid _validId;
    private string _validName = null!;
    private DateTime _validDate;

    [SetUp]
    public void SetUp()
    {
        _validId = Guid.NewGuid();
        _validDate = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _validName = "Backend-1";
    }
    
    [Test]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var id = Guid.Empty;
        
        var action = () => new Team(id, _validName, _validDate);
        
        action.Should()
            .Throw<ArgumentException>("Пустой идентификатор команды должен выкидывать ошибку")
            .WithParameterName("id");
    }

    [TestCase("", TestName = "Constructor_EmptyStringName_ThrowsArgumentException")]
    [TestCase(null, TestName = "Constructor_NullStringName_ThrowsArgumentException")]
    [TestCase(" ", TestName = "Constructor_WhitespaceName_ThrowsArgumentException")]
    [TestCase("\t\n", TestName = "Constructor_WhitespaceCharactersName_ThrowsArgumentException")]
    public void Constructor_InvalidName_ThrowsArgumentException(string? name)
    {
        var action = () => new Team(_validId, name!, _validDate);
        
        action.Should()
            .Throw<ArgumentException>("некорректное название команды должно выкидывать ошибку")
            .WithParameterName("name");
    }

    [TestCase(DateTimeKind.Unspecified)]
    [TestCase(DateTimeKind.Local)]
    public void Constructor_DateNotUtc_ThrowsArgumentException(DateTimeKind kind)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, kind);
        
        var action = () => new Team(_validId, _validName, createdAt);
        
        action.Should()
            .Throw<ArgumentException>("Дата не в формате UTC должна выкидывать ошибку")
            .WithParameterName("createdAt");
    }

    [TestCase("Backend-1 ", "Backend-1",  TestName = "NameWithWhiteSpaceInRight_ReturnsWithoutWhiteSpace")]
    [TestCase("Backend-1  ", "Backend-1", TestName = "NameWithTwoWhiteSpaceInRight_ReturnsWithoutWhiteSpace")]
    [TestCase(" Backend-1", "Backend-1", TestName = "NameWithWhiteSpaceInLeft_ReturnsWithoutWhiteSpace")]
    [TestCase("  Backend-1", "Backend-1", TestName = "NameWithTwoWhiteSpaceInLeft_ReturnsWithoutWhiteSpace")]
    [TestCase(" Backend-1 ", "Backend-1", TestName = "NameWithWhiteSpaceInRightAndLeft_ReturnsWithoutWhiteSpace")]
    public void Constructor_ShouldTrimName(string name, string expectedName)
    {
        var team = new Team(_validId, name, _validDate);
        
        team.Name.Should()
            .Be(expectedName, "Конструктор должен обрезать пробелы с начала и конца имени команды");
    }
    
    [Test]
    public void Constructor_ValidArguments_CreatesTeam()
    {
        var team = new Team(_validId, _validName, _validDate);
        
        team.Id.Should().Be(_validId, $"Id команды должен совпадать с заданным: {_validId}");
        team.Name.Should().Be(_validName, $"Name команды должно совпадать с заданным: {_validName}");
        team.CreatedAt.Should().Be(_validDate, $"CreatedAt команды должен совпадать с заданным: {_validDate}");
    }
    
    [TestCase("", TestName = "Rename_EmptyString_ThrowsArgumentException")]
    [TestCase(null, TestName = "Rename_NullString_ThrowsArgumentException")]
    [TestCase(" ", TestName = "Rename_WhitespaceName_ThrowsArgumentException")]
    [TestCase("\t\n", TestName = "Rename_WhitespaceCharactersName_ThrowsArgumentException")]
    public void Rename_InvalidName_ThrowsArgumentException(string? newName)
    {
        var team = new Team(_validId, _validName, _validDate);
        
        var action = () => team.Rename(newName!);
        
        action.Should()
            .Throw<ArgumentException>("некорректное имя при переименовании должно выкидывать ошибку")
            .WithParameterName("name");
    }

    [TestCase("", TestName = "Rename_EmptyString_DontChangeOldName")]
    [TestCase(null, TestName = "Rename_NullString_DontChangeOldName")]
    [TestCase(" ", TestName = "Rename_WhitespaceName_DontChangeOldName")]
    [TestCase("\t\n", TestName = "Rename_WhitespaceCharactersName_DontChangeOldName")]
    public void Rename_InvalidName_DontChangeOldName(string? newName)
    {
        var team = new Team(_validId, _validName, _validDate);
        
        var act = () => team.Rename(newName!);

        act.Should().Throw<ArgumentException>();
        team.Name.Should().Be(_validName, "Неудачное переименование не должно менять старое имя");
    }
    
    [TestCase("Backend-2", "Backend-2")]
    [TestCase(" Backend-2 ", "Backend-2")]
    [TestCase("  Backend-2", "Backend-2")]
    [TestCase("Backend-2  ", "Backend-2")]
    [TestCase("Backend 2", "Backend 2")]
    public void Rename_ValidName_UpdatesName(string newName, string expectedName)
    {
        var team = new Team(_validId, _validName, _validDate);

        team.Rename(newName);

        team.Name.Should().Be(expectedName);
        team.Id.Should().Be(_validId);
        team.CreatedAt.Should().Be(_validDate);
    }
}
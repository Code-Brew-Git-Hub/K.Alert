using FluentAssertions;
using ReminderService.Domain.Entities;
using ReminderService.Domain.Enums;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class DeliveryChannelTests
{
    private static readonly Guid ChannelId = Guid.NewGuid();
    private static readonly Guid TeamId = Guid.NewGuid();

    private static readonly DateTime CreatedAt =
        new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    private const string Settings = """{"chatId":"-1001234567890"}""";

    [Test]
    public void Constructor_ValidArguments_CreatesEnabledChannel()
    {
        var channel = CreateChannel();

        channel.Id.Should().Be(ChannelId);
        channel.TeamId.Should().Be(TeamId);
        channel.Name.Should().Be("Telegram команды");
        channel.Type.Should().Be(ChannelType.Telegram);
        channel.Settings.Should().Be(Settings);
        channel.CreatedAt.Should().Be(CreatedAt);
        channel.IsEnabled.Should().BeTrue();
        channel.DeletedAt.Should().BeNull();
    }

    [TestCase(true, TestName = "Constructor_EmptyId_Throws")]
    [TestCase(false, TestName = "Constructor_EmptyTeamId_Throws")]
    public void Constructor_EmptyIdentifier_Throws(bool emptyId)
    {
        var id = emptyId ? Guid.Empty : ChannelId;
        var teamId = emptyId ? TeamId : Guid.Empty;

        var action = () => new DeliveryChannel(
            id,
            teamId,
            "Telegram команды",
            ChannelType.Telegram,
            Settings,
            CreatedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(emptyId ? "id" : "teamId");
    }

    [TestCase(null, TestName = "Constructor_NullName_Throws")]
    [TestCase("", TestName = "Constructor_EmptyName_Throws")]
    [TestCase("   ", TestName = "Constructor_WhitespaceName_Throws")]
    public void Constructor_InvalidName_Throws(string? name)
    {
        Action action = () => CreateChannel(name!);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [TestCase(null, TestName = "Constructor_NullSettings_Throws")]
    [TestCase("", TestName = "Constructor_EmptySettings_Throws")]
    [TestCase("   ", TestName = "Constructor_WhitespaceSettings_Throws")]
    public void Constructor_InvalidSettings_Throws(string? settings)
    {
        var action = () => CreateChannel(settings: settings!);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("settings");
    }

    [Test]
    public void Constructor_UnknownType_Throws()
    {
        var action = () => new DeliveryChannel(
            ChannelId,
            TeamId,
            "Telegram команды",
            (ChannelType)999,
            Settings,
            CreatedAt);

        action.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("type");
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Constructor_NonUtcDate_Throws(DateTimeKind kind)
    {
        var createdAt = DateTime.SpecifyKind(CreatedAt, kind);

        Action action = () => CreateChannel(createdAt: createdAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("createdAt");
    }

    [Test]
    public void Constructor_NameWithSurroundingWhitespace_TrimsName()
    {
        var channel = CreateChannel("  Telegram команды  ");

        channel.Name.Should().Be("Telegram команды");
    }

    [Test]
    public void Constructor_SettingsWithSurroundingWhitespace_PreservesSettings()
    {
        var settings = $"  {Settings}  ";

        var channel = CreateChannel(settings: settings);

        channel.Settings.Should().Be(settings);
    }

    [Test]
    public void Rename_ValidName_ChangesAndTrimsName()
    {
        var channel = CreateChannel();

        channel.Rename("  Новое название  ");

        channel.Name.Should().Be("Новое название");
    }

    [TestCase(null, TestName = "Rename_NullName_ThrowsAndPreservesState")]
    [TestCase("", TestName = "Rename_EmptyName_ThrowsAndPreservesState")]
    [TestCase("   ", TestName = "Rename_WhitespaceName_ThrowsAndPreservesState")]
    public void Rename_InvalidName_ThrowsAndPreservesState(string? name)
    {
        var channel = CreateChannel();
        var expected = CreateChannel();

        var action = () => channel.Rename(name!);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("name");

        channel.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void ChangeSettings_ValidSettings_ReplacesSettingsWithoutTrimming()
    {
        var channel = CreateChannel();
        const string newSettings = """  {"chatId":"-1009999999999"}  """;

        channel.ChangeSettings(newSettings);

        channel.Settings.Should().Be(newSettings);
    }

    [TestCase(null, TestName = "ChangeSettings_Null_ThrowsAndPreservesState")]
    [TestCase("", TestName = "ChangeSettings_Empty_ThrowsAndPreservesState")]
    [TestCase("   ", TestName = "ChangeSettings_Whitespace_ThrowsAndPreservesState")]
    public void ChangeSettings_InvalidSettings_ThrowsAndPreservesState(
        string? settings)
    {
        var channel = CreateChannel();
        var expected = CreateChannel();

        var action = () => channel.ChangeSettings(settings!);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("settings");

        channel.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void Disable_EnabledChannel_DisablesChannel()
    {
        var channel = CreateChannel();

        channel.Disable();

        channel.IsEnabled.Should().BeFalse();
        channel.DeletedAt.Should().BeNull();
    }

    [Test]
    public void Enable_DisabledChannel_EnablesChannel()
    {
        var channel = CreateChannel();
        channel.Disable();

        channel.Enable();

        channel.IsEnabled.Should().BeTrue();
        channel.DeletedAt.Should().BeNull();
    }

    [TestCase(true, TestName = "Enable_AlreadyEnabled_PreservesState")]
    [TestCase(false, TestName = "Disable_AlreadyDisabled_PreservesState")]
    public void SetEnabled_RepeatedCall_PreservesState(bool enabled)
    {
        var channel = CreateChannel();
        var expected = CreateChannel();

        if (enabled)
        {
            channel.Enable();
            channel.Enable();
        }
        else
        {
            expected.Disable();
            channel.Disable();
            channel.Disable();
        }

        channel.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void DisabledChannel_AllowsRenameAndSettingsChange()
    {
        var channel = CreateChannel();
        channel.Disable();
        const string newSettings = """{"chatId":"123"}""";

        channel.Rename("Новое название");
        channel.ChangeSettings(newSettings);

        channel.Name.Should().Be("Новое название");
        channel.Settings.Should().Be(newSettings);
        channel.IsEnabled.Should().BeFalse();
    }

    [TestCase(0, TestName = "Delete_AtCreationTime_DeletesChannel")]
    [TestCase(60, TestName = "Delete_AfterCreation_DeletesChannel")]
    public void Delete_ValidDate_SetsDeletedAtAndDisablesChannel(int minutes)
    {
        var channel = CreateChannel();
        var deletedAt = CreatedAt.AddMinutes(minutes);

        channel.Delete(deletedAt);

        channel.DeletedAt.Should().Be(deletedAt);
        channel.IsEnabled.Should().BeFalse();
    }

    [Test]
    public void Delete_DisabledChannel_MarksChannelDeleted()
    {
        var channel = CreateChannel();
        channel.Disable();
        var deletedAt = CreatedAt.AddHours(1);

        channel.Delete(deletedAt);

        channel.DeletedAt.Should().Be(deletedAt);
        channel.IsEnabled.Should().BeFalse();
    }

    [Test]
    public void Delete_AlreadyDeleted_PreservesOriginalState()
    {
        var channel = CreateChannel();
        var expected = CreateChannel();
        var deletedAt = CreatedAt.AddHours(1);

        channel.Delete(deletedAt);
        expected.Delete(deletedAt);

        channel.Delete(deletedAt.AddHours(1));

        channel.Should().BeEquivalentTo(expected);
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Delete_NonUtcDate_ThrowsAndPreservesState(DateTimeKind kind)
    {
        var channel = CreateChannel();
        var expected = CreateChannel();
        var deletedAt = DateTime.SpecifyKind(CreatedAt.AddHours(1), kind);

        var action = () => channel.Delete(deletedAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("deletedAt");

        channel.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void Delete_DateBeforeCreation_ThrowsAndPreservesState()
    {
        var channel = CreateChannel();
        var expected = CreateChannel();

        var action = () => channel.Delete(CreatedAt.AddTicks(-1));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("deletedAt");

        channel.Should().BeEquivalentTo(expected);
    }

    [TestCase("Rename")]
    [TestCase("ChangeSettings")]
    [TestCase("Enable")]
    [TestCase("Disable")]
    public void Modify_DeletedChannel_ThrowsAndPreservesState(string operation)
    {
        var channel = CreateChannel();
        var expected = CreateChannel();
        var deletedAt = CreatedAt.AddHours(1);

        channel.Delete(deletedAt);
        expected.Delete(deletedAt);

        Action action = operation switch
        {
            "Rename" => () => channel.Rename("Новое название"),
            "ChangeSettings" => () =>
                channel.ChangeSettings("""{"chatId":"123"}"""),
            "Enable" => () => channel.Enable(),
            "Disable" => () => channel.Disable(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        action.Should().Throw<InvalidOperationException>();

        channel.Should().BeEquivalentTo(expected);
    }

    private static DeliveryChannel CreateChannel(
        string name = "Telegram команды",
        string settings = Settings,
        DateTime? createdAt = null)
    {
        return new DeliveryChannel(
            ChannelId,
            TeamId,
            name,
            ChannelType.Telegram,
            settings,
            createdAt ?? CreatedAt);
    }
}
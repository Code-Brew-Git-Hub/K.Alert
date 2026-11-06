using FluentAssertions;
using ReminderService.Domain.Entities;
using ReminderService.Domain.Enums;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class ReminderTests
{
    private static readonly Guid ReminderId = Guid.NewGuid();
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid AuthorId = Guid.NewGuid();

    private static readonly DateTime CreatedAt =
        new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime FirstOccurrenceAt = CreatedAt.AddHours(1);

    private static readonly DateOnly StartDate = new(2026, 11, 5);
    private static readonly TimeOnly LocalTime = new(16, 0);

    private const string TimeZoneId = "Asia/Yekaterinburg";

    [Test]
    public void Constructor_ValidArguments_CreatesActiveReminder()
    {
        var reminder = Create();

        reminder.Should().BeEquivalentTo(new
        {
            Id = ReminderId,
            TeamId,
            Title = "Напоминание",
            Text = "Текст сообщения",
            StartLocalDate = StartDate,
            LocalTime,
            TimeZoneId,
            RepeatUnit = RepeatUnit.None,
            RepeatInterval = (int?)null,
            EndLocalDate = (DateOnly?)null,
            NextOccurrenceAt = (DateTime?)FirstOccurrenceAt,
            Status = ReminderStatus.Active,
            CreatedByUserId = AuthorId,
            CreatedAt,
            UpdatedAt = CreatedAt,
            Version = 1L
        });
    }

    [TestCase("id", TestName = "Constructor_EmptyId_Throws")]
    [TestCase("teamId", TestName = "Constructor_EmptyTeamId_Throws")]
    [TestCase(
        "createdByUserId",
        TestName = "Constructor_EmptyCreatedByUserId_Throws")]
    public void Constructor_EmptyIdentifier_Throws(string parameterName)
    {
        Action action = () => Create(
            parameterName == "id" ? Guid.Empty : ReminderId,
            parameterName == "teamId" ? Guid.Empty : TeamId,
            createdByUserId: parameterName == "createdByUserId"
                ? Guid.Empty
                : AuthorId);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [TestCase(null, "title", TestName = "Constructor_NullTitle_Throws")]
    [TestCase("", "title", TestName = "Constructor_EmptyTitle_Throws")]
    [TestCase("   ", "title", TestName = "Constructor_WhitespaceTitle_Throws")]
    [TestCase(null, "text", TestName = "Constructor_NullText_Throws")]
    [TestCase("", "text", TestName = "Constructor_EmptyText_Throws")]
    [TestCase("   ", "text", TestName = "Constructor_WhitespaceText_Throws")]
    public void Constructor_InvalidContent_Throws(
        string? value,
        string parameterName)
    {
        Action action = () => Create(
            title: parameterName == "title" ? value! : "Название",
            text: parameterName == "text" ? value! : "Текст");

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [Test]
    public void Constructor_TrimsTitleAndPreservesTextFormatting()
    {
        var reminder = Create(
            title: "  Напоминание  ",
            text: "  Первая строка\nВторая строка  ");

        reminder.Title.Should().Be("Напоминание");
        reminder.Text.Should().Be("  Первая строка\nВторая строка  ");
    }

    [TestCase(null, TestName = "Constructor_NullTimeZoneId_Throws")]
    [TestCase("", TestName = "Constructor_EmptyTimeZoneId_Throws")]
    [TestCase("   ", TestName = "Constructor_WhitespaceTimeZoneId_Throws")]
    public void Constructor_InvalidTimeZoneId_Throws(string? timeZoneId)
    {
        Action action = () => Create(timeZoneId: timeZoneId!);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("timeZoneId");
    }

    [Test]
    public void Constructor_UnknownRepeatUnit_Throws()
    {
        Action action = () => Create(repeatUnit: (RepeatUnit)999);

        action.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("repeatUnit");
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    public void Constructor_OneTimeReminderWithInterval_Throws(int interval)
    {
        Action action = () => Create(
            repeatUnit: RepeatUnit.None,
            repeatInterval: interval);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("repeatInterval");
    }

    [Test]
    public void Constructor_RepeatingReminderWithoutPositiveInterval_Throws()
    {
        foreach (RepeatUnit unit in Enum.GetValues(typeof(RepeatUnit)))
        {
            if (unit == RepeatUnit.None)
                continue;

            foreach (var interval in new int?[] { null, -1, 0 })
            {
                Action action = () => Create(
                    repeatUnit: unit,
                    repeatInterval: interval);

                action.Should()
                    .Throw<ArgumentException>(
                        $"единица повторения: {unit}, интервал: {interval}")
                    .WithParameterName("repeatInterval");
            }
        }
    }

    [Test]
    public void Constructor_RepeatingReminderWithPositiveInterval_Succeeds()
    {
        foreach (RepeatUnit unit in Enum.GetValues(typeof(RepeatUnit)))
        {
            if (unit == RepeatUnit.None)
                continue;

            var reminder = Create(repeatUnit: unit, repeatInterval: 2);

            reminder.RepeatUnit.Should().Be(unit);
            reminder.RepeatInterval.Should().Be(2);
        }
    }

    [Test]
    public void Constructor_EndDateBeforeStartDate_Throws()
    {
        Action action = () => Create(
            endLocalDate: StartDate.AddDays(-1));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("endLocalDate");
    }

    [Test]
    public void Constructor_EndDateEqualsStartDate_Succeeds()
    {
        var reminder = Create(endLocalDate: StartDate);

        reminder.EndLocalDate.Should().Be(StartDate);
    }

    [TestCase("createdAt", DateTimeKind.Local)]
    [TestCase("createdAt", DateTimeKind.Unspecified)]
    [TestCase("nextOccurrenceAt", DateTimeKind.Local)]
    [TestCase("nextOccurrenceAt", DateTimeKind.Unspecified)]
    public void Constructor_NonUtcDate_Throws(
        string parameterName,
        DateTimeKind kind)
    {
        Action action = () => Create(
            createdAt: parameterName == "createdAt"
                ? DateTime.SpecifyKind(CreatedAt, kind)
                : CreatedAt,
            nextOccurrenceAt: parameterName == "nextOccurrenceAt"
                ? DateTime.SpecifyKind(FirstOccurrenceAt, kind)
                : FirstOccurrenceAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [Test]
    public void UpdateContent_ValidArguments_UpdatesContentAndVersion()
    {
        var reminder = Create();
        var updatedAt = CreatedAt.AddMinutes(1);

        reminder.UpdateContent("  Новое название  ", "  Новый текст\n", updatedAt);

        reminder.Title.Should().Be("Новое название");
        reminder.Text.Should().Be("  Новый текст\n");
        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(2);
        reminder.NextOccurrenceAt.Should().Be(FirstOccurrenceAt);
        reminder.Status.Should().Be(ReminderStatus.Active);
    }

    [TestCase(null, "title")]
    [TestCase("", "title")]
    [TestCase("   ", "title")]
    [TestCase(null, "text")]
    [TestCase("", "text")]
    [TestCase("   ", "text")]
    public void UpdateContent_InvalidContent_DoesNotChangeState(
        string? value,
        string parameterName)
    {
        var reminder = Create();
        var before = Snapshot(reminder);

        var action = () => reminder.UpdateContent(
            parameterName == "title" ? value! : "Новое название",
            parameterName == "text" ? value! : "Новый текст",
            CreatedAt.AddMinutes(1));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(parameterName);

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [Test]
    public void ChangeSchedule_ActiveReminder_UpdatesScheduleAndVersion()
    {
        var reminder = Create();
        var newStartDate = StartDate.AddDays(1);
        var newTime = new TimeOnly(18, 30);
        var nextOccurrenceAt = FirstOccurrenceAt.AddDays(1);
        var updatedAt = CreatedAt.AddMinutes(1);

        reminder.ChangeSchedule(
            newStartDate,
            newTime,
            "UTC",
            RepeatUnit.None,
            null,
            newStartDate,
            nextOccurrenceAt,
            updatedAt);

        reminder.StartLocalDate.Should().Be(newStartDate);
        reminder.LocalTime.Should().Be(newTime);
        reminder.TimeZoneId.Should().Be("UTC");
        reminder.RepeatUnit.Should().Be(RepeatUnit.None);
        reminder.RepeatInterval.Should().BeNull();
        reminder.EndLocalDate.Should().Be(newStartDate);
        reminder.NextOccurrenceAt.Should().Be(nextOccurrenceAt);
        reminder.Status.Should().Be(ReminderStatus.Active);
        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(2);
    }

    [Test]
    public void ChangeSchedule_PausedReminder_KeepsPausedState()
    {
        var reminder = Create();
        reminder.Pause(CreatedAt.AddMinutes(1));

        var updatedAt = CreatedAt.AddMinutes(2);

        ChangeSchedule(reminder, null, updatedAt);

        reminder.Status.Should().Be(ReminderStatus.Paused);
        reminder.NextOccurrenceAt.Should().BeNull();
        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(3);
    }

    [TestCase(false, TestName = "ChangeSchedule_ActiveWithoutNextOccurrence_Throws")]
    [TestCase(true, TestName = "ChangeSchedule_PausedWithNextOccurrence_Throws")]
    public void ChangeSchedule_InconsistentNextOccurrence_DoesNotChangeState(
        bool paused)
    {
        var reminder = Create();

        if (paused)
            reminder.Pause(CreatedAt.AddMinutes(1));

        var before = Snapshot(reminder);

        var action = () => ChangeSchedule(
            reminder,
            paused ? FirstOccurrenceAt : null,
            CreatedAt.AddMinutes(2));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("nextOccurrenceAt");

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [Test]
    public void ChangeSchedule_InvalidSchedule_DoesNotChangeState()
    {
        var reminder = Create();
        var before = Snapshot(reminder);

        var action = () => reminder.ChangeSchedule(
            StartDate,
            LocalTime,
            TimeZoneId,
            RepeatUnit.None,
            1,
            null,
            FirstOccurrenceAt,
            CreatedAt.AddMinutes(1));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("repeatInterval");

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [Test]
    public void Pause_ActiveReminder_ClearsNextOccurrenceAndUpdatesVersion()
    {
        var reminder = Create();
        var updatedAt = CreatedAt.AddMinutes(1);

        reminder.Pause(updatedAt);

        reminder.Status.Should().Be(ReminderStatus.Paused);
        reminder.NextOccurrenceAt.Should().BeNull();
        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(2);
    }

    [Test]
    public void Pause_AlreadyPaused_DoesNotChangeState()
    {
        var reminder = Create();
        reminder.Pause(CreatedAt.AddMinutes(1));
        var before = Snapshot(reminder);

        reminder.Pause(CreatedAt.AddMinutes(2));

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [Test]
    public void Resume_PausedReminder_ActivatesAndUpdatesVersion()
    {
        var reminder = Create();
        reminder.Pause(CreatedAt.AddMinutes(1));

        var nextOccurrenceAt = FirstOccurrenceAt.AddDays(1);
        var updatedAt = CreatedAt.AddMinutes(2);

        reminder.Resume(nextOccurrenceAt, updatedAt);

        reminder.Status.Should().Be(ReminderStatus.Active);
        reminder.NextOccurrenceAt.Should().Be(nextOccurrenceAt);
        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(3);
    }

    [Test]
    public void Resume_PausedReminderWithoutNextOccurrence_CompletesReminder()
    {
        var reminder = Create();
        reminder.Pause(CreatedAt.AddMinutes(1));
        var previousVersion = reminder.Version;
        var updatedAt = FirstOccurrenceAt.AddDays(1);

        reminder.Resume(null, updatedAt);

        reminder.Status.Should().Be(ReminderStatus.Completed);
        reminder.NextOccurrenceAt.Should().BeNull();
        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(previousVersion + 1);
    }

    [Test]
    public void AdvanceSchedule_RepeatingReminder_UpdatesNextOccurrence()
    {
        var reminder = CreateRepeating();
        var nextOccurrenceAt = FirstOccurrenceAt.AddDays(1);
        var updatedAt = FirstOccurrenceAt;

        reminder.AdvanceSchedule(nextOccurrenceAt, updatedAt);

        reminder.Status.Should().Be(ReminderStatus.Active);
        reminder.NextOccurrenceAt.Should().Be(nextOccurrenceAt);
        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(2);
    }

    [TestCase(false, TestName = "AdvanceSchedule_OneTimeWithoutNext_Completes")]
    [TestCase(true, TestName = "AdvanceSchedule_RepeatingWithoutNext_Completes")]
    public void AdvanceSchedule_WithoutNextOccurrence_CompletesReminder(
        bool repeating)
    {
        var reminder = repeating ? CreateRepeating() : Create();

        reminder.AdvanceSchedule(null, FirstOccurrenceAt);

        reminder.Status.Should().Be(ReminderStatus.Completed);
        reminder.NextOccurrenceAt.Should().BeNull();
        reminder.UpdatedAt.Should().Be(FirstOccurrenceAt);
        reminder.Version.Should().Be(2);
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void AdvanceSchedule_NextOccurrenceNotLater_DoesNotChangeState(
        int seconds)
    {
        var reminder = CreateRepeating();
        var before = Snapshot(reminder);

        var action = () => reminder.AdvanceSchedule(
            FirstOccurrenceAt.AddSeconds(seconds),
            FirstOccurrenceAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("nextOccurrenceAt");

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [Test]
    public void AdvanceSchedule_OneTimeWithNextOccurrence_DoesNotChangeState()
    {
        var reminder = Create();
        var before = Snapshot(reminder);

        var action = () => reminder.AdvanceSchedule(
            FirstOccurrenceAt.AddDays(1),
            FirstOccurrenceAt);

        action.Should().Throw<InvalidOperationException>();

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [TestCase(ReminderStatus.Active)]
    [TestCase(ReminderStatus.Paused)]
    [TestCase(ReminderStatus.Completed)]
    public void Delete_Reminder_DeletesAndUpdatesVersion(ReminderStatus status)
    {
        var reminder = CreateInStatus(status);
        var previousVersion = reminder.Version;
        var updatedAt = CreatedAt.AddDays(1);

        reminder.Delete(updatedAt);

        reminder.Status.Should().Be(ReminderStatus.Deleted);
        reminder.NextOccurrenceAt.Should().BeNull();
        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(previousVersion + 1);
    }

    [Test]
    public void Delete_AlreadyDeleted_DoesNotChangeState()
    {
        var reminder = CreateInStatus(ReminderStatus.Deleted);
        var before = Snapshot(reminder);

        reminder.Delete(CreatedAt.AddDays(1));

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [TestCase(ReminderStatus.Deleted, "UpdateContent")]
    [TestCase(ReminderStatus.Completed, "ChangeSchedule")]
    [TestCase(ReminderStatus.Deleted, "ChangeSchedule")]
    [TestCase(ReminderStatus.Completed, "Pause")]
    [TestCase(ReminderStatus.Deleted, "Pause")]
    [TestCase(ReminderStatus.Active, "Resume")]
    [TestCase(ReminderStatus.Completed, "Resume")]
    [TestCase(ReminderStatus.Deleted, "Resume")]
    [TestCase(ReminderStatus.Paused, "AdvanceSchedule")]
    [TestCase(ReminderStatus.Completed, "AdvanceSchedule")]
    [TestCase(ReminderStatus.Deleted, "AdvanceSchedule")]
    public void Operation_InForbiddenStatus_DoesNotChangeState(
        ReminderStatus status,
        string operation)
    {
        var reminder = CreateInStatus(status);
        var before = Snapshot(reminder);

        var action = () => Execute(
            reminder,
            operation,
            CreatedAt.AddDays(1));

        action.Should().Throw<InvalidOperationException>();

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [TestCase("UpdateContent")]
    [TestCase("ChangeSchedule")]
    [TestCase("Pause")]
    [TestCase("Resume")]
    [TestCase("AdvanceSchedule")]
    [TestCase("Delete")]
    public void Operation_TimeBeforePreviousChange_DoesNotChangeState(
        string operation)
    {
        var reminder = CreateForOperation(operation);
        var before = Snapshot(reminder);
        var invalidTime = reminder.UpdatedAt.AddTicks(-1);

        var action = () => Execute(reminder, operation, invalidTime);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("updatedAt");

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [TestCase("UpdateContent", DateTimeKind.Local)]
    [TestCase("UpdateContent", DateTimeKind.Unspecified)]
    [TestCase("ChangeSchedule", DateTimeKind.Local)]
    [TestCase("ChangeSchedule", DateTimeKind.Unspecified)]
    [TestCase("Pause", DateTimeKind.Local)]
    [TestCase("Pause", DateTimeKind.Unspecified)]
    [TestCase("Resume", DateTimeKind.Local)]
    [TestCase("Resume", DateTimeKind.Unspecified)]
    [TestCase("AdvanceSchedule", DateTimeKind.Local)]
    [TestCase("AdvanceSchedule", DateTimeKind.Unspecified)]
    [TestCase("Delete", DateTimeKind.Local)]
    [TestCase("Delete", DateTimeKind.Unspecified)]
    public void Operation_NonUtcChangeTime_DoesNotChangeState(
        string operation,
        DateTimeKind kind)
    {
        var reminder = CreateForOperation(operation);
        var before = Snapshot(reminder);
        var invalidTime = DateTime.SpecifyKind(
            CreatedAt.AddDays(1),
            kind);

        var action = () => Execute(reminder, operation, invalidTime);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("updatedAt");

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    [TestCase("UpdateContent")]
    [TestCase("ChangeSchedule")]
    [TestCase("Pause")]
    [TestCase("Resume")]
    [TestCase("AdvanceSchedule")]
    [TestCase("Delete")]
    public void Operation_TimeEqualsPreviousChange_Succeeds(string operation)
    {
        var reminder = CreateForOperation(operation);
        var previousVersion = reminder.Version;
        var updatedAt = reminder.UpdatedAt;

        Execute(reminder, operation, updatedAt);

        reminder.UpdatedAt.Should().Be(updatedAt);
        reminder.Version.Should().Be(previousVersion + 1);
    }

    [TestCase("ChangeSchedule", DateTimeKind.Local)]
    [TestCase("ChangeSchedule", DateTimeKind.Unspecified)]
    [TestCase("Resume", DateTimeKind.Local)]
    [TestCase("Resume", DateTimeKind.Unspecified)]
    [TestCase("AdvanceSchedule", DateTimeKind.Local)]
    [TestCase("AdvanceSchedule", DateTimeKind.Unspecified)]
    public void Operation_NonUtcNextOccurrence_DoesNotChangeState(
        string operation,
        DateTimeKind kind)
    {
        var reminder = CreateForOperation(operation);
        var before = Snapshot(reminder);
        var nextOccurrenceAt = DateTime.SpecifyKind(
            FirstOccurrenceAt.AddDays(1),
            kind);
        var updatedAt = CreatedAt.AddMinutes(2);

        var action = () =>
        {
            switch (operation)
            {
                case "ChangeSchedule":
                    ChangeSchedule(reminder, nextOccurrenceAt, updatedAt);
                    break;

                case "Resume":
                    reminder.Resume(nextOccurrenceAt, updatedAt);
                    break;

                case "AdvanceSchedule":
                    reminder.AdvanceSchedule(nextOccurrenceAt, updatedAt);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        };

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("nextOccurrenceAt");

        Snapshot(reminder).Should().BeEquivalentTo(before);
    }

    private static Reminder Create(
        Guid? id = null,
        Guid? teamId = null,
        string title = "Напоминание",
        string text = "Текст сообщения",
        string timeZoneId = TimeZoneId,
        RepeatUnit repeatUnit = RepeatUnit.None,
        int? repeatInterval = null,
        DateOnly? endLocalDate = null,
        DateTime? nextOccurrenceAt = null,
        Guid? createdByUserId = null,
        DateTime? createdAt = null)
    {
        return new Reminder(
            id ?? ReminderId,
            teamId ?? TeamId,
            title,
            text,
            StartDate,
            LocalTime,
            timeZoneId,
            repeatUnit,
            repeatInterval,
            endLocalDate,
            nextOccurrenceAt ?? FirstOccurrenceAt,
            createdByUserId ?? AuthorId,
            createdAt ?? CreatedAt);
    }

    private static Reminder CreateRepeating()
    {
        // Используем любое существующее значение повторения, кроме None.
        foreach (RepeatUnit unit in Enum.GetValues(typeof(RepeatUnit)))
            if (unit != RepeatUnit.None)
                return Create(repeatUnit: unit, repeatInterval: 1);

        throw new InvalidOperationException(
            "В RepeatUnit отсутствуют варианты периодического расписания.");
    }

    private static Reminder CreateInStatus(ReminderStatus status)
    {
        var reminder = Create();

        switch (status)
        {
            case ReminderStatus.Active:
                break;

            case ReminderStatus.Paused:
                reminder.Pause(CreatedAt.AddMinutes(1));
                break;

            case ReminderStatus.Completed:
                reminder.AdvanceSchedule(null, FirstOccurrenceAt);
                break;

            case ReminderStatus.Deleted:
                reminder.Delete(CreatedAt.AddMinutes(1));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return reminder;
    }

    private static Reminder CreateForOperation(string operation)
    {
        var reminder = operation == "AdvanceSchedule"
            ? CreateRepeating()
            : Create();

        // Предыдущее изменение нужно для проверки относительно UpdatedAt,
        // а не только относительно CreatedAt.
        reminder.UpdateContent(
            reminder.Title,
            reminder.Text,
            CreatedAt.AddMinutes(1));

        if (operation == "Resume")
            reminder.Pause(CreatedAt.AddMinutes(1));

        return reminder;
    }

    private static void ChangeSchedule(
        Reminder reminder,
        DateTime? nextOccurrenceAt,
        DateTime updatedAt)
    {
        reminder.ChangeSchedule(
            StartDate.AddDays(1),
            new TimeOnly(18, 0),
            "UTC",
            RepeatUnit.None,
            null,
            null,
            nextOccurrenceAt,
            updatedAt);
    }

    private static void Execute(
        Reminder reminder,
        string operation,
        DateTime updatedAt)
    {
        switch (operation)
        {
            case "UpdateContent":
                reminder.UpdateContent("Новое название", "Новый текст", updatedAt);
                break;

            case "ChangeSchedule":
                ChangeSchedule(
                    reminder,
                    FirstOccurrenceAt.AddDays(1),
                    updatedAt);
                break;

            case "Pause":
                reminder.Pause(updatedAt);
                break;

            case "Resume":
                reminder.Resume(FirstOccurrenceAt.AddDays(1), updatedAt);
                break;

            case "AdvanceSchedule":
                reminder.AdvanceSchedule(
                    FirstOccurrenceAt.AddDays(1),
                    updatedAt);
                break;

            case "Delete":
                reminder.Delete(updatedAt);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static object Snapshot(Reminder reminder)
    {
        return new
        {
            reminder.Id,
            reminder.TeamId,
            reminder.Title,
            reminder.Text,
            reminder.StartLocalDate,
            reminder.LocalTime,
            reminder.TimeZoneId,
            reminder.RepeatUnit,
            reminder.RepeatInterval,
            reminder.EndLocalDate,
            reminder.NextOccurrenceAt,
            reminder.Status,
            reminder.CreatedByUserId,
            reminder.CreatedAt,
            reminder.UpdatedAt,
            reminder.Version
        };
    }
}
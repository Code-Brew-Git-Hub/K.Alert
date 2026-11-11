using FluentAssertions;
using ReminderService.Domain.Entities;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class ReminderOccurrenceTests
{
    private static readonly DateTime ScheduledAt =
        new(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Constructor_WhenIdIsEmpty_ShouldThrow()
    {
        Action action = () => Create(Guid.Empty);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("id");
    }

    [Test]
    public void Constructor_WhenReminderIdIsEmpty_ShouldThrow()
    {
        Action action = () => Create(reminderId: Guid.Empty);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("reminderId");
    }

    [TestCase(null, TestName = "Constructor_NullTitle_ShouldThrow")]
    [TestCase("", TestName = "Constructor_EmptyTitle_ShouldThrow")]
    [TestCase(" \t\r\n ", TestName = "Constructor_WhitespaceTitle_ShouldThrow")]
    public void Constructor_WhenTitleIsInvalid_ShouldThrow(string? title)
    {
        Action action = () => Create(titleSnapshot: title);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("titleSnapshot");
    }

    [TestCase(null, TestName = "Constructor_NullText_ShouldThrow")]
    [TestCase("", TestName = "Constructor_EmptyText_ShouldThrow")]
    [TestCase(" \t\r\n ", TestName = "Constructor_WhitespaceText_ShouldThrow")]
    public void Constructor_WhenTextIsInvalid_ShouldThrow(string? text)
    {
        Action action = () => Create(textSnapshot: text);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("textSnapshot");
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Constructor_WhenScheduledAtIsNotUtc_ShouldThrow(
        DateTimeKind kind)
    {
        var scheduledAt = DateTime.SpecifyKind(ScheduledAt, kind);

        Action action = () => Create(scheduledAt: scheduledAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("scheduledAt");
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Constructor_WhenCreatedAtIsNotUtc_ShouldThrow(
        DateTimeKind kind)
    {
        var createdAt = DateTime.SpecifyKind(ScheduledAt, kind);

        Action action = () => Create(createdAt: createdAt);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("createdAt");
    }

    [Test]
    public void Constructor_WhenCreatedBeforeScheduledAt_ShouldThrow()
    {
        Action action = () => Create(
            createdAt: ScheduledAt.AddTicks(-1));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("createdAt");
    }

    [TestCase(0, TestName = "Constructor_CreatedExactlyAtScheduledTime_ShouldSucceed")]
    [TestCase(120, TestName = "Constructor_CreatedWithDelay_ShouldSucceed")]
    public void Constructor_WhenArgumentsAreValid_ShouldSaveAllFields(
        int delaySeconds)
    {
        var id = Guid.NewGuid();
        var reminderId = Guid.NewGuid();
        var createdAt = ScheduledAt.AddSeconds(delaySeconds);

        var occurrence = new ReminderOccurrence(
            id,
            reminderId,
            ScheduledAt,
            "Ротация секрета",
            "Нужно обновить секрет.",
            createdAt);

        occurrence.Id.Should().Be(id);
        occurrence.ReminderId.Should().Be(reminderId);
        occurrence.ScheduledAt.Should().Be(ScheduledAt);
        occurrence.TitleSnapshot.Should().Be("Ротация секрета");
        occurrence.TextSnapshot.Should().Be("Нужно обновить секрет.");
        occurrence.CreatedAt.Should().Be(createdAt);
    }

    [Test]
    public void Constructor_ShouldTrimTitle()
    {
        var occurrence = Create(
            titleSnapshot: " \t Ротация секрета \r\n ");

        occurrence.TitleSnapshot.Should().Be("Ротация секрета");
    }

    [Test]
    public void Constructor_ShouldPreserveTextFormatting()
    {
        const string text =
            "  Первая строка\n\n\tВторая строка  \n";

        var occurrence = Create(textSnapshot: text);

        occurrence.TextSnapshot.Should().Be(text);
    }

    private static ReminderOccurrence Create(
        Guid? id = null,
        Guid? reminderId = null,
        DateTime? scheduledAt = null,
        string? titleSnapshot = "Ротация секрета",
        string? textSnapshot = "Нужно обновить секрет.",
        DateTime? createdAt = null)
    {
        return new ReminderOccurrence(
            id ?? Guid.NewGuid(),
            reminderId ?? Guid.NewGuid(),
            scheduledAt ?? ScheduledAt,
            titleSnapshot!,
            textSnapshot!,
            createdAt ?? ScheduledAt);
    }
}
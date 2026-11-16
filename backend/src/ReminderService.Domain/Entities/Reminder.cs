using ReminderService.Domain.Enums;
using ReminderService.Domain.Scheduling;

namespace ReminderService.Domain.Entities;

public class Reminder
{
    public Reminder(
        Guid id,
        Guid teamId,
        string title,
        string text,
        DateOnly startLocalDate,
        TimeOnly localTime,
        string timeZoneId,
        RepeatUnit repeatUnit,
        int? repeatInterval,
        DateOnly? endLocalDate,
        Guid createdByUserId,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор напоминания не должен быть пустым.",
                nameof(id));

        if (teamId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор команды не должен быть пустым.",
                nameof(teamId));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор автора не должен быть пустым.",
                nameof(createdByUserId));

        ValidateContent(title, text);

        var schedule = new ReminderSchedule(
            startLocalDate,
            localTime,
            timeZoneId,
            repeatUnit,
            repeatInterval,
            endLocalDate);

        var nextOccurrenceAt = ScheduleCalculator.FindOnOrAfter(schedule, createdAt)
                               ?? throw new ArgumentException(
                                   "В расписании нет срабатываний на момент создания или позже.");

        Id = id;
        TeamId = teamId;
        Title = title.Trim();
        Text = text;

        StartLocalDate = startLocalDate;
        LocalTime = localTime;
        TimeZoneId = timeZoneId;
        RepeatUnit = repeatUnit;
        RepeatInterval = repeatInterval;
        EndLocalDate = endLocalDate;

        NextOccurrenceAt = nextOccurrenceAt;
        Status = ReminderStatus.Active;

        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string Title { get; private set; }
    public string Text { get; private set; }

    public DateOnly StartLocalDate { get; private set; }
    public TimeOnly LocalTime { get; private set; }
    public string TimeZoneId { get; private set; }
    public RepeatUnit RepeatUnit { get; private set; }
    public int? RepeatInterval { get; private set; }
    public DateOnly? EndLocalDate { get; private set; }

    public DateTimeOffset? NextOccurrenceAt { get; private set; }
    public ReminderStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public long Version { get; private set; }

    public void UpdateContent(
        string title,
        string text,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        ValidateContent(title, text);
        ValidateChangeTime(updatedAt);

        Title = title.Trim();
        Text = text;

        Touch(updatedAt);
    }

    public void ChangeSchedule(
        DateOnly startLocalDate,
        TimeOnly localTime,
        string timeZoneId,
        RepeatUnit repeatUnit,
        int? repeatInterval,
        DateOnly? endLocalDate,
        DateTimeOffset updatedAt)
    {
        EnsureScheduleEditable();

        ValidateChangeTime(updatedAt);

        DateTimeOffset? nextOccurrenceAt = null;

        if (Status == ReminderStatus.Active)
        {
            var schedule = new ReminderSchedule(
                startLocalDate,
                localTime,
                timeZoneId,
                repeatUnit,
                repeatInterval,
                endLocalDate);

            nextOccurrenceAt = ScheduleCalculator.FindOnOrAfter(schedule, updatedAt)
                               ?? throw new ArgumentException(
                                   "В новом расписании нет срабатываний на момент изменения или позже.");
        }
        else
        {
            ReminderSchedule.Validate(
                startLocalDate,
                timeZoneId,
                repeatUnit,
                repeatInterval,
                endLocalDate);
        }

        StartLocalDate = startLocalDate;
        LocalTime = localTime;
        TimeZoneId = timeZoneId;
        RepeatUnit = repeatUnit;
        RepeatInterval = repeatInterval;
        EndLocalDate = endLocalDate;
        NextOccurrenceAt = nextOccurrenceAt;

        Touch(updatedAt);
    }

    public void Pause(DateTimeOffset updatedAt)
    {
        EnsureScheduleEditable();
        ValidateChangeTime(updatedAt);

        if (Status == ReminderStatus.Paused)
            return;

        Status = ReminderStatus.Paused;
        NextOccurrenceAt = null;

        Touch(updatedAt);
    }

    public void Resume(DateTimeOffset updatedAt)
    {
        if (Status != ReminderStatus.Paused)
            throw new InvalidOperationException(
                "Возобновить можно только напоминание на паузе.");

        ValidateChangeTime(updatedAt);

        var schedule = new ReminderSchedule(
            StartLocalDate,
            LocalTime,
            TimeZoneId,
            RepeatUnit,
            RepeatInterval,
            EndLocalDate);

        var nextOccurrenceAt = ScheduleCalculator.FindOnOrAfter(schedule, updatedAt);

        NextOccurrenceAt = nextOccurrenceAt;
        Status = nextOccurrenceAt.HasValue
            ? ReminderStatus.Active
            : ReminderStatus.Completed;

        Touch(updatedAt);
    }

    public void AdvanceSchedule(DateTimeOffset updatedAt)
    {
        if (Status != ReminderStatus.Active)
            throw new InvalidOperationException(
                "Продвинуть можно только активное расписание.");

        ValidateChangeTime(updatedAt);

        var currentOccurrenceAt = NextOccurrenceAt!.Value;
        if (updatedAt < currentOccurrenceAt)
            throw new InvalidOperationException(
                "Время текущего срабатывания ещё не наступило.");

        var schedule = new ReminderSchedule(
            StartLocalDate,
            LocalTime,
            TimeZoneId,
            RepeatUnit,
            RepeatInterval,
            EndLocalDate);

        NextOccurrenceAt = ScheduleCalculator.FindAfter(schedule, currentOccurrenceAt);

        if (!NextOccurrenceAt.HasValue)
            Status = ReminderStatus.Completed;

        Touch(updatedAt);
    }

    public void Delete(DateTimeOffset updatedAt)
    {
        ValidateChangeTime(updatedAt);

        if (Status == ReminderStatus.Deleted)
            return;

        Status = ReminderStatus.Deleted;
        NextOccurrenceAt = null;

        Touch(updatedAt);
    }

    private void EnsureNotDeleted()
    {
        if (Status == ReminderStatus.Deleted)
            throw new InvalidOperationException(
                "Нельзя изменить удалённое напоминание.");
    }

    private void EnsureScheduleEditable()
    {
        if (Status is not (ReminderStatus.Active or ReminderStatus.Paused))
            throw new InvalidOperationException(
                "Операция доступна только для активного напоминания " +
                "или напоминания на паузе.");
    }

    private void ValidateChangeTime(DateTimeOffset updatedAt)
    {
        if (updatedAt < UpdatedAt)
            throw new ArgumentException(
                "Дата изменения не может быть раньше предыдущего изменения.",
                nameof(updatedAt));
    }

    private void Touch(DateTimeOffset updatedAt)
    {
        UpdatedAt = updatedAt;
        Version++;
    }

    private static void ValidateContent(string title, string text)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "Название напоминания не должно быть пустым.",
                nameof(title));

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(
                "Текст напоминания не должен быть пустым.",
                nameof(text));
    }
}

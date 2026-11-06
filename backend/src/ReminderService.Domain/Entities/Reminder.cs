using ReminderService.Domain.Scheduling;
using ReminderService.Domain.Enums;

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
        DateTime nextOccurrenceAt,
        Guid createdByUserId,
        DateTime createdAt)
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
        ReminderSchedule.Validate(
            startLocalDate,
            timeZoneId,
            repeatUnit,
            repeatInterval,
            endLocalDate);

        ValidateUtc(createdAt, nameof(createdAt));
        ValidateUtc(nextOccurrenceAt, nameof(nextOccurrenceAt));

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

    public DateTime? NextOccurrenceAt { get; private set; }
    public ReminderStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public long Version { get; private set; }

    public void UpdateContent(
        string title,
        string text,
        DateTime updatedAt)
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
        DateTime? nextOccurrenceAt,
        DateTime updatedAt)
    {
        EnsureScheduleEditable();

        ReminderSchedule.Validate(
            startLocalDate,
            timeZoneId,
            repeatUnit,
            repeatInterval,
            endLocalDate);

        ValidateChangeTime(updatedAt);

        if (Status == ReminderStatus.Active)
        {
            if (!nextOccurrenceAt.HasValue)
                throw new ArgumentException(
                    "Для активного напоминания нужно следующее срабатывание.",
                    nameof(nextOccurrenceAt));

            ValidateUtc(nextOccurrenceAt.Value, nameof(nextOccurrenceAt));
        }
        else if (nextOccurrenceAt.HasValue)
        {
            throw new ArgumentException(
                "У напоминания на паузе следующее срабатывание отсутствует.",
                nameof(nextOccurrenceAt));
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

    public void Pause(DateTime updatedAt)
    {
        EnsureScheduleEditable();
        ValidateChangeTime(updatedAt);

        if (Status == ReminderStatus.Paused)
            return;

        Status = ReminderStatus.Paused;
        NextOccurrenceAt = null;

        Touch(updatedAt);
    }

    public void Resume(DateTime? nextOccurrenceAt, DateTime updatedAt)
    {
        if (Status != ReminderStatus.Paused)
            throw new InvalidOperationException(
                "Возобновить можно только напоминание на паузе.");

        if (nextOccurrenceAt.HasValue)
            ValidateUtc(nextOccurrenceAt.Value, nameof(nextOccurrenceAt));

        ValidateChangeTime(updatedAt);

        NextOccurrenceAt = nextOccurrenceAt;
        Status = nextOccurrenceAt.HasValue
            ? ReminderStatus.Active
            : ReminderStatus.Completed;

        Touch(updatedAt);
    }

    public void AdvanceSchedule(DateTime? nextOccurrenceAt, DateTime updatedAt)
    {
        if (Status != ReminderStatus.Active)
            throw new InvalidOperationException(
                "Продвинуть можно только активное расписание.");

        ValidateChangeTime(updatedAt);

        var currentOccurrenceAt = NextOccurrenceAt!.Value;

        if (updatedAt < currentOccurrenceAt)
            throw new InvalidOperationException(
                "Время текущего срабатывания ещё не наступило.");

        if (nextOccurrenceAt.HasValue)
        {
            ValidateUtc(nextOccurrenceAt.Value, nameof(nextOccurrenceAt));

            if (nextOccurrenceAt.Value <= currentOccurrenceAt)
                throw new ArgumentException(
                    "Следующее срабатывание должно быть позже текущего.",
                    nameof(nextOccurrenceAt));

            if (RepeatUnit == RepeatUnit.None)
            {
                var schedule = new ReminderSchedule(
                    StartLocalDate,
                    LocalTime,
                    TimeZoneId,
                    RepeatUnit,
                    RepeatInterval,
                    EndLocalDate);

                if (ScheduleCalculator.FindOnOrAfter(schedule, currentOccurrenceAt) != currentOccurrenceAt
                    || ScheduleCalculator.FindAfter(schedule, currentOccurrenceAt) != nextOccurrenceAt)
                {
                    throw new InvalidOperationException(
                        "Следующее срабатывание разового напоминания допустимо только при повторе местного времени.");
                }
            }
        }

        NextOccurrenceAt = nextOccurrenceAt;

        if (!nextOccurrenceAt.HasValue)
            Status = ReminderStatus.Completed;

        Touch(updatedAt);
    }

    public void Delete(DateTime updatedAt)
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

    private void ValidateChangeTime(DateTime updatedAt)
    {
        ValidateUtc(updatedAt, nameof(updatedAt));

        if (updatedAt < UpdatedAt)
            throw new ArgumentException(
                "Дата изменения не может быть раньше предыдущего изменения.",
                nameof(updatedAt));
    }

    private void Touch(DateTime updatedAt)
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

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Дата должна быть указана в UTC.",
                parameterName);
    }
}
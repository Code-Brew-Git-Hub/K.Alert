namespace ReminderService.Domain.Entities;

public class ReminderOccurrence
{
    public ReminderOccurrence(
        Guid id,
        Guid reminderId,
        DateTimeOffset scheduledAt,
        string titleSnapshot,
        string textSnapshot,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор срабатывания не должен быть пустым.",
                nameof(id));

        if (reminderId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор напоминания не должен быть пустым.",
                nameof(reminderId));

        if (string.IsNullOrWhiteSpace(titleSnapshot))
            throw new ArgumentException(
                "Название срабатывания не должно быть пустым.",
                nameof(titleSnapshot));

        if (string.IsNullOrWhiteSpace(textSnapshot))
            throw new ArgumentException(
                "Текст срабатывания не должен быть пустым.",
                nameof(textSnapshot));

        if (createdAt < scheduledAt)
            throw new ArgumentException(
                "Срабатывание нельзя создать раньше запланированного времени.",
                nameof(createdAt));

        Id = id;
        ReminderId = reminderId;
        ScheduledAt = scheduledAt;
        TitleSnapshot = titleSnapshot.Trim();
        TextSnapshot = textSnapshot;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ReminderId { get; private set; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public string TitleSnapshot { get; private set; }
    public string TextSnapshot { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
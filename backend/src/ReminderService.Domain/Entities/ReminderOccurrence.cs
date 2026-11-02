namespace ReminderService.Domain.Entities;

public class ReminderOccurrence
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string TitleSnapshot { get; set; }
    public string TextSnapshot { get; set; }
    public DateTime CreatedAt { get; set; }
}
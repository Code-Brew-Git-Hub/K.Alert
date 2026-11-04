using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Entities;

public class Reminder
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public DateOnly StartLocalDate { get; set; }
    public TimeOnly LocalTime { get; set; }
    public string TimeZoneId { get; set; }
    public RepeatUnit RepeatUnit { get; set; }
    public int? RepeatInterval { get; set; }
    public DateOnly? EndLocalDate { get; set; }
    public DateTime? NextOccurrenceAt { get; set; }
    public ReminderStatus Status { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Version { get; set; }
}
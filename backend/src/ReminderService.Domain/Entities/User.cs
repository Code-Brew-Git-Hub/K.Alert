namespace ReminderService.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}
using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Entities;

public class TeamMember
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public TeamRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
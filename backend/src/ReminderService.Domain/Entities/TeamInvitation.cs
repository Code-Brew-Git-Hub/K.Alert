namespace ReminderService.Domain.Entities;

public class TeamInvitation
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string TokenHash { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
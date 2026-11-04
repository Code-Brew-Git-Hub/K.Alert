using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Entities;

public class Delivery
{
    public Guid Id { get; set; }
    public Guid OccurrenceId { get; set; }
    public Guid ChannelId { get; set; }
    public ChannelType ChannelTypeSnapshot { get; set; }
    public string DestinationSnapshot { get; set; }
    public DeliveryStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public Guid? ClaimToken { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
    public string? ExternalMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
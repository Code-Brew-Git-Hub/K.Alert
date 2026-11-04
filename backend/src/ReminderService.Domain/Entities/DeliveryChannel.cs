using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Entities;

public class DeliveryChannel
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string Name { get; set; }
    public ChannelType Type { get; set; }
    public string Settings { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
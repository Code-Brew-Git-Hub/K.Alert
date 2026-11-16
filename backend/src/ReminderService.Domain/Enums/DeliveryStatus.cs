namespace ReminderService.Domain.Enums;

public enum DeliveryStatus
{
    Pending = 0,
    Processing = 1,
    RetryScheduled = 2,
    Sent = 3,
    Blocked = 4,
    Cancelled = 5
}

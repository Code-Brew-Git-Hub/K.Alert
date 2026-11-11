using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Entities;

public class Delivery
{
    public Delivery(
        Guid id,
        Guid occurrenceId,
        Guid channelId,
        ChannelType channelTypeSnapshot,
        string destinationSnapshot,
        DateTimeOffset createdAt)
    {
        ValidateId(id, nameof(id));
        ValidateId(occurrenceId, nameof(occurrenceId));
        ValidateId(channelId, nameof(channelId));

        if (!Enum.IsDefined(channelTypeSnapshot))
            throw new ArgumentOutOfRangeException(
                nameof(channelTypeSnapshot),
                channelTypeSnapshot,
                "Указан неизвестный тип канала.");

        if (string.IsNullOrWhiteSpace(destinationSnapshot))
            throw new ArgumentException(
                "Назначение доставки не должно быть пустым.",
                nameof(destinationSnapshot));

        Id = id;
        OccurrenceId = occurrenceId;
        ChannelId = channelId;
        ChannelTypeSnapshot = channelTypeSnapshot;
        DestinationSnapshot = destinationSnapshot;

        Status = DeliveryStatus.Pending;
        AttemptCount = 0;
        NextAttemptAt = createdAt;

        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid OccurrenceId { get; private set; }
    public Guid ChannelId { get; private set; }

    public ChannelType ChannelTypeSnapshot { get; private set; }
    public string DestinationSnapshot { get; private set; }

    public DeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public Guid? ClaimToken { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }
    public string? LastError { get; private set; }
    public string? ExternalMessageId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Claim(
        Guid claimToken,
        DateTimeOffset leaseUntil,
        DateTimeOffset now)
    {
        ValidateId(claimToken, nameof(claimToken));
        ValidateChangeTime(now);

        if (leaseUntil <= now)
            throw new ArgumentException(
                "Срок захвата должен заканчиваться позже текущего времени.",
                nameof(leaseUntil));

        var isReady =
            Status is DeliveryStatus.Pending
                or DeliveryStatus.RetryScheduled
            && NextAttemptAt.HasValue
            && NextAttemptAt.Value <= now;

        var isLeaseExpired =
            Status == DeliveryStatus.Processing
            && LeaseUntil.HasValue
            && LeaseUntil.Value <= now;

        if (!isReady && !isLeaseExpired)
            throw new InvalidOperationException(
                "Доставка пока недоступна для захвата.");

        if (ClaimToken == claimToken)
            throw new ArgumentException(
                "Для нового захвата нужен новый токен.",
                nameof(claimToken));

        var nextAttemptCount = checked(AttemptCount + 1);

        Status = DeliveryStatus.Processing;
        ClaimToken = claimToken;
        LeaseUntil = leaseUntil;
        NextAttemptAt = null;
        AttemptCount = nextAttemptCount;
        UpdatedAt = now;
    }

    public void MarkSent(
        Guid claimToken,
        DateTimeOffset now,
        string? externalMessageId = null)
    {
        EnsureActiveClaim(claimToken, now);

        Status = DeliveryStatus.Sent;
        SentAt = now;
        ExternalMessageId = externalMessageId;
        LastError = null;

        ClearClaim();
        UpdatedAt = now;
    }

    public void ScheduleRetry(
        Guid claimToken,
        string error,
        DateTimeOffset nextAttemptAt,
        DateTimeOffset now)
    {
        EnsureActiveClaim(claimToken, now);
        ValidateError(error);

        if (nextAttemptAt <= now)
            throw new ArgumentException(
                "Повторная попытка должна быть назначена на будущее.",
                nameof(nextAttemptAt));

        Status = DeliveryStatus.RetryScheduled;
        LastError = error;
        NextAttemptAt = nextAttemptAt;

        ClearClaim();
        UpdatedAt = now;
    }

    public void Block(
        Guid claimToken,
        string error,
        DateTimeOffset now)
    {
        EnsureActiveClaim(claimToken, now);
        ValidateError(error);

        Status = DeliveryStatus.Blocked;
        LastError = error;
        NextAttemptAt = null;

        ClearClaim();
        UpdatedAt = now;
    }

    public void Resume(DateTimeOffset now)
    {
        ValidateChangeTime(now);

        if (Status != DeliveryStatus.Blocked)
            throw new InvalidOperationException(
                "Возобновить можно только заблокированную доставку.");

        Status = DeliveryStatus.RetryScheduled;
        NextAttemptAt = now;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        ValidateChangeTime(now);

        if (Status == DeliveryStatus.Sent)
            throw new InvalidOperationException(
                "Нельзя отменить уже отправленную доставку.");

        if (Status == DeliveryStatus.Cancelled)
            return;

        Status = DeliveryStatus.Cancelled;
        NextAttemptAt = null;

        ClearClaim();
        UpdatedAt = now;
    }

    private void EnsureActiveClaim(Guid claimToken, DateTimeOffset now)
    {
        ValidateId(claimToken, nameof(claimToken));
        ValidateChangeTime(now);

        if (Status != DeliveryStatus.Processing)
            throw new InvalidOperationException(
                "Доставка не находится в обработке.");

        if (ClaimToken != claimToken)
            throw new InvalidOperationException(
                "Токен захвата не соответствует текущему.");

        if (!LeaseUntil.HasValue || LeaseUntil.Value <= now)
            throw new InvalidOperationException(
                "Срок захвата доставки истёк.");
    }

    private void ClearClaim()
    {
        ClaimToken = null;
        LeaseUntil = null;
    }

    private void ValidateChangeTime(DateTimeOffset now)
    {
        if (now < UpdatedAt)
            throw new ArgumentException(
                "Время операции не может быть раньше предыдущего изменения.",
                nameof(now));
    }

    private static void ValidateId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор не должен быть пустым.",
                parameterName);
    }

    private static void ValidateError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException(
                "Описание ошибки не должно быть пустым.",
                nameof(error));
    }
}
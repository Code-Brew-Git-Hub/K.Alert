namespace ReminderService.Domain.Entities;

public class TeamInvitation
{
    public TeamInvitation(
        Guid id,
        Guid teamId,
        string tokenHash,
        Guid createdByUserId,
        DateTime createdAt,
        DateTime expiresAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор приглашения не должен быть пустым.",
                nameof(id));

        if (teamId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор команды не должен быть пустым.",
                nameof(teamId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException(
                "Хеш токена не должен быть пустым.",
                nameof(tokenHash));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор автора не должен быть пустым.",
                nameof(createdByUserId));

        ValidateUtc(createdAt, nameof(createdAt));
        ValidateUtc(expiresAt, nameof(expiresAt));

        if (expiresAt <= createdAt)
            throw new ArgumentException(
                "Срок действия должен заканчиваться позже даты создания.",
                nameof(expiresAt));

        Id = id;
        TeamId = teamId;
        TokenHash = tokenHash;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string TokenHash { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; }
    public Guid? AcceptedByUserId { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public void Accept(Guid userId, DateTime acceptedAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор пользователя не должен быть пустым.",
                nameof(userId));

        ValidateActionTime(acceptedAt, nameof(acceptedAt));

        if (AcceptedAt.HasValue)
            throw new InvalidOperationException(
                "Приглашение уже использовано.");

        if (RevokedAt.HasValue)
            throw new InvalidOperationException(
                "Приглашение отозвано.");

        if (acceptedAt >= ExpiresAt)
            throw new InvalidOperationException(
                "Срок действия приглашения истёк.");

        AcceptedByUserId = userId;
        AcceptedAt = acceptedAt;
    }

    public void Revoke(DateTime revokedAt)
    {
        ValidateActionTime(revokedAt, nameof(revokedAt));

        if (AcceptedAt.HasValue)
            throw new InvalidOperationException(
                "Нельзя отозвать использованное приглашение.");

        if (RevokedAt.HasValue)
            return;

        RevokedAt = revokedAt;
    }

    private void ValidateActionTime(DateTime value, string parameterName)
    {
        ValidateUtc(value, parameterName);

        if (value < CreatedAt)
            throw new ArgumentException(
                "Дата действия не может быть раньше даты создания.",
                parameterName);
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Дата должна быть указана в UTC.",
                parameterName);
    }
}
using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Entities;

public class DeliveryChannel
{
    public DeliveryChannel(
        Guid id,
        Guid teamId,
        string name,
        ChannelType type,
        string settings,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор канала не должен быть пустым.",
                nameof(id));

        if (teamId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор команды не должен быть пустым.",
                nameof(teamId));

        ValidateName(name);
        ValidateSettings(settings);

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Указан неизвестный тип канала.");

        Id = id;
        TeamId = teamId;
        Name = name.Trim();
        Type = type;
        Settings = settings;
        IsEnabled = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string Name { get; private set; }
    public ChannelType Type { get; private set; }
    public string Settings { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void Rename(string name)
    {
        EnsureNotDeleted();
        ValidateName(name);

        Name = name.Trim();
    }

    public void ChangeSettings(string settings)
    {
        EnsureNotDeleted();
        ValidateSettings(settings);

        Settings = settings;
    }

    public void Enable()
    {
        EnsureNotDeleted();

        IsEnabled = true;
    }

    public void Disable()
    {
        EnsureNotDeleted();

        IsEnabled = false;
    }

    public void Delete(DateTimeOffset deletedAt)
    {
        if (deletedAt < CreatedAt)
            throw new ArgumentException(
                "Дата удаления не может быть раньше даты создания.",
                nameof(deletedAt));

        if (DeletedAt.HasValue)
            return;

        DeletedAt = deletedAt;
        IsEnabled = false;
    }

    private void EnsureNotDeleted()
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException(
                "Нельзя изменить удалённый канал.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Название канала не должно быть пустым.",
                nameof(name));
    }

    private static void ValidateSettings(string settings)
    {
        if (string.IsNullOrWhiteSpace(settings))
            throw new ArgumentException(
                "Настройки канала не должны быть пустыми.",
                nameof(settings));
    }
}

namespace ReminderService.Domain.Entities;

public class User
{
    public User(Guid id, string displayName, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор пользователя не должен быть пустым.",
                nameof(id));

        ValidateDisplayName(displayName);

        Id = id;
        DisplayName = displayName.Trim();
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string DisplayName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Rename(string displayName)
    {
        ValidateDisplayName(displayName);

        DisplayName = displayName.Trim();
    }

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException(
                "Имя пользователя не должно быть пустым.",
                nameof(displayName));
    }
}

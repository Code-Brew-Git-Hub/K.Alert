namespace ReminderService.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }

    public User(Guid id, string displayName, DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор пользователя не должен быть пустым.",
                nameof(id));

        ValidateDisplayName(displayName);

        if (createdAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Дата создания должна быть указана в UTC.",
                nameof(createdAt));

        Id = id;
        DisplayName = displayName.Trim();
        CreatedAt = createdAt;
    }
    
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
namespace ReminderService.Domain.Entities;

public class Team
{
    public Team(Guid id, string name, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор команды не должен быть пустым.",
                nameof(id));

        ValidateName(name);

        Id = id;
        Name = name.Trim();
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Rename(string name)
    {
        ValidateName(name);

        Name = name.Trim();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Название команды не должно быть пустым.",
                nameof(name));
    }
}
using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Entities;

public class TeamMember
{
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public TeamRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    
    public TeamMember(
        Guid teamId,
        Guid userId,
        TeamRole role,
        DateTime joinedAt)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор команды не должен быть пустым.",
                nameof(teamId));

        if (userId == Guid.Empty)
            throw new ArgumentException(
                "Идентификатор пользователя не должен быть пустым.",
                nameof(userId));

        ValidateRole(role);

        if (joinedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Дата вступления должна быть указана в UTC.",
                nameof(joinedAt));

        TeamId = teamId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    public void ChangeRole(TeamRole role)
    {
        ValidateRole(role);

        Role = role;
    }

    private static void ValidateRole(TeamRole role)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Указана неизвестная роль участника команды.");
    }
}
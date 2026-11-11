using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Scheduling;

public sealed record ReminderSchedule(
    DateOnly StartLocalDate,
    TimeOnly LocalTime,
    string TimeZoneId,
    RepeatUnit RepeatUnit,
    int? RepeatInterval,
    DateOnly? EndLocalDate)
{
    internal static TimeZoneInfo Validate(
        DateOnly startLocalDate,
        string timeZoneId,
        RepeatUnit repeatUnit,
        int? repeatInterval,
        DateOnly? endLocalDate)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException(
                "Часовой пояс не должен быть пустым.",
                nameof(timeZoneId));

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
            throw new ArgumentException(
                "Указан неизвестный или недоступный часовой пояс.",
                nameof(timeZoneId));

        if (!Enum.IsDefined(repeatUnit))
            throw new ArgumentOutOfRangeException(
                nameof(repeatUnit),
                repeatUnit,
                "Указана неизвестная единица повторения.");

        if (repeatUnit == RepeatUnit.None)
        {
            if (repeatInterval.HasValue)
                throw new ArgumentException(
                    "Для разового напоминания интервал должен отсутствовать.",
                    nameof(repeatInterval));
        }
        else if (!repeatInterval.HasValue || repeatInterval.Value < 1)
        {
            throw new ArgumentException(
                "Интервал повторения должен быть не меньше единицы.",
                nameof(repeatInterval));
        }

        if (endLocalDate.HasValue && endLocalDate.Value < startLocalDate)
            throw new ArgumentException(
                "Дата окончания не может быть раньше даты начала.",
                nameof(endLocalDate));

        return timeZone;
    }
}

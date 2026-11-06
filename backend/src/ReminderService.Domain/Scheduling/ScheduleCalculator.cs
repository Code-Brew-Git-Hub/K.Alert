using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Scheduling;

public static class ScheduleCalculator
{
    public static DateTime? FindOnOrAfter(
        ReminderSchedule schedule,
        DateTime fromUtc)
    {
        ValidateUtc(fromUtc, nameof(fromUtc));
        return Find(schedule, fromUtc, includeBoundary: true);
    }

    public static DateTime? FindAfter(
        ReminderSchedule schedule,
        DateTime afterUtc)
    {
        ValidateUtc(afterUtc, nameof(afterUtc));
        return Find(schedule, afterUtc, includeBoundary: false);
    }

    private static DateTime? Find(
        ReminderSchedule schedule,
        DateTime boundaryUtc,
        bool includeBoundary)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        ReminderSchedule.Validate(
            schedule.StartLocalDate,
            schedule.TimeZoneId,
            schedule.RepeatUnit,
            schedule.RepeatInterval,
            schedule.EndLocalDate);

        var zone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
        var start = schedule.StartLocalDate;
        var end = schedule.EndLocalDate ?? DateOnly.MaxValue;
        var utcDay = DateOnly.FromDateTime(boundaryUtc).DayNumber;

        // Из-за смещения часового пояса локальная дата может
        // приходиться на предыдущий день относительно даты в UTC.
        var earliestDate = DateOnly.FromDayNumber(
            Math.Max(start.DayNumber, utcDay - 1));

        if (earliestDate > end)
            return null;

        var interval = (long)(schedule.RepeatInterval ?? 1);

        if (schedule.RepeatUnit == RepeatUnit.Week)
            interval *= 7;

        var firstIndex = Distance(start, earliestDate, schedule.RepeatUnit) / interval;
        var lastIndex = Distance(start, end, schedule.RepeatUnit) / interval;
        var offsets = GetOffsets(zone);
        DateTime? best = null;

        for (var index = firstIndex; index <= lastIndex; index++)
        {
            var date = GetDate(start, schedule.RepeatUnit, (int)(index * interval));

            if (date > end || (best.HasValue
                && date.DayNumber > DateOnly.FromDateTime(best.Value).DayNumber + 1))
                break;

            var localTime = date.ToDateTime(schedule.LocalTime);

            foreach (var candidate in GetUtcCandidates(localTime, zone, offsets))
            {
                var matchesBoundary =
                    candidate > boundaryUtc || (includeBoundary && candidate == boundaryUtc);

                if (matchesBoundary && (!best.HasValue || candidate < best.Value))
                    best = candidate;
            }
        }

        return best;
    }

    private static int Distance(DateOnly start, DateOnly end, RepeatUnit unit) =>
        unit switch
        {
            RepeatUnit.Day or RepeatUnit.Week => end.DayNumber - start.DayNumber,
            RepeatUnit.Month => (end.Year - start.Year) * 12 + end.Month - start.Month,
            RepeatUnit.Year => end.Year - start.Year,
            _ => 0
        };

    private static DateOnly GetDate(DateOnly start, RepeatUnit unit, int distance) =>
        unit switch
        {
            RepeatUnit.Day or RepeatUnit.Week => start.AddDays(distance),
            RepeatUnit.Month => start.AddMonths(distance),
            RepeatUnit.Year => start.AddYears(distance),
            _ => start
        };

    private static HashSet<TimeSpan> GetOffsets(TimeZoneInfo zone)
    {
        var offsets = new HashSet<TimeSpan> { zone.BaseUtcOffset };

        foreach (var rule in zone.GetAdjustmentRules())
        {
            var standard = zone.BaseUtcOffset + rule.BaseUtcOffsetDelta;
            offsets.Add(standard);
            offsets.Add(standard + rule.DaylightDelta);
        }

        return offsets;
    }

    private static IEnumerable<DateTime> GetUtcCandidates(
        DateTime localTime,
        TimeZoneInfo zone,
        HashSet<TimeSpan> offsets)
    {
        DateTime? shifted = null;
        var exactMatch = false;

        foreach (var offset in offsets)
        {
            var candidate = ToUtc(localTime, offset);

            if (!candidate.HasValue)
                continue;

            var actualOffset = zone.GetUtcOffset(candidate.Value);

            if (actualOffset == offset)
            {
                exactMatch = true;
                yield return candidate.Value;
            }
            else if (actualOffset > offset)
            {
                // При скачке вперёд старое смещение даёт момент после
                // перевода часов, а новое даёт момент до перевода.
                var beforeTransition = ToUtc(localTime, actualOffset);

                if (beforeTransition.HasValue
                    && zone.GetUtcOffset(beforeTransition.Value) == offset
                    && (!shifted.HasValue || candidate.Value < shifted.Value))
                {
                    shifted = candidate;
                }
            }
        }

        if (!exactMatch && shifted.HasValue)
            yield return shifted.Value;
    }

    private static DateTime? ToUtc(DateTime localTime, TimeSpan offset)
    {
        var ticks = localTime.Ticks - offset.Ticks;

        return ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks
            ? new DateTime(ticks, DateTimeKind.Utc)
            : null;
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Дата должна быть указана в UTC.",
                parameterName);
    }
}
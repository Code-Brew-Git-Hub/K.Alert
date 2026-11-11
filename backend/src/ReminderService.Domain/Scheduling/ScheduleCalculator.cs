using ReminderService.Domain.Enums;

namespace ReminderService.Domain.Scheduling;

public static class ScheduleCalculator
{
    public static DateTimeOffset? FindOnOrAfter(
        ReminderSchedule schedule,
        DateTimeOffset fromUtc)
    {
        return Find(schedule, fromUtc, true);
    }

    public static DateTimeOffset? FindAfter(
        ReminderSchedule schedule,
        DateTimeOffset afterUtc)
    {
        return Find(schedule, afterUtc, false);
    }

    private static DateTimeOffset? Find(
        ReminderSchedule schedule,
        DateTimeOffset boundaryUtc,
        bool includeBoundary)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var zone = ReminderSchedule.Validate(
            schedule.StartLocalDate,
            schedule.TimeZoneId,
            schedule.RepeatUnit,
            schedule.RepeatInterval,
            schedule.EndLocalDate);

        var start = schedule.StartLocalDate;
        var end = schedule.EndLocalDate ?? DateOnly.MaxValue;
        var utcDay = DateOnly.FromDateTime(boundaryUtc.UtcDateTime).DayNumber;

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
        DateTimeOffset? best = null;

        for (var index = firstIndex; index <= lastIndex; index++)
        {
            var date = GetDate(start, schedule.RepeatUnit, (int)(index * interval));

            if (date > end || (best.HasValue
                               && date.DayNumber > DateOnly.FromDateTime(best.Value.UtcDateTime).DayNumber + 1))
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

    private static int Distance(DateOnly start, DateOnly end, RepeatUnit unit)
    {
        return unit switch
        {
            RepeatUnit.Day or RepeatUnit.Week => end.DayNumber - start.DayNumber,
            RepeatUnit.Month => (end.Year - start.Year) * 12 + end.Month - start.Month,
            RepeatUnit.Year => end.Year - start.Year,
            _ => 0
        };
    }

    private static DateOnly GetDate(DateOnly start, RepeatUnit unit, int distance)
    {
        return unit switch
        {
            RepeatUnit.Day or RepeatUnit.Week => start.AddDays(distance),
            RepeatUnit.Month => start.AddMonths(distance),
            RepeatUnit.Year => start.AddYears(distance),
            _ => start
        };
    }

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

    private static IEnumerable<DateTimeOffset> GetUtcCandidates(
        DateTime localTime,
        TimeZoneInfo zone,
        HashSet<TimeSpan> offsets)
    {
        DateTimeOffset? shifted = null;
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
                    shifted = candidate;
            }
        }

        if (!exactMatch && shifted.HasValue)
            yield return shifted.Value;
    }

    private static DateTimeOffset? ToUtc(DateTime localTime, TimeSpan offset)
    {
        var ticks = localTime.Ticks - offset.Ticks;

        return ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks
            ? new DateTimeOffset(ticks, TimeSpan.Zero)
            : null;
    }
}
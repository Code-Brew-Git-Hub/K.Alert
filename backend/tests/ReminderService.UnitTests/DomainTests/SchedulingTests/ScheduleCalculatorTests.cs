using FluentAssertions;
using ReminderService.Domain.Enums;
using ReminderService.Domain.Scheduling;

namespace ReminderService.UnitTests.DomainTests.SchedulingTests;

[TestFixture]
public class ScheduleCalculatorTests
{
    [Test]
    public void FindOnOrAfter_AtOccurrence_ReturnsSameInstant()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2026, 11, 15), new TimeOnly(9, 0),
            "Asia/Yekaterinburg", RepeatUnit.None, null, null);
        var occurrence = Utc(2026, 11, 15, 4);

        ScheduleCalculator.FindOnOrAfter(schedule, occurrence)
            .Should().Be(occurrence);
        ScheduleCalculator.FindAfter(schedule, occurrence)
            .Should().BeNull();
    }

    [Test]
    public void MonthlySchedule_ReturnsToOriginalDayAfterShortMonth()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2025, 1, 31), new TimeOnly(9, 0),
            "UTC", RepeatUnit.Month, 1, new DateOnly(2025, 4, 30));

        var january = Utc(2025, 1, 31, 9);
        var february = Utc(2025, 2, 28, 9);
        var march = Utc(2025, 3, 31, 9);
        var april = Utc(2025, 4, 30, 9);

        ScheduleCalculator.FindAfter(schedule, january).Should().Be(february);
        ScheduleCalculator.FindAfter(schedule, february).Should().Be(march);
        ScheduleCalculator.FindAfter(schedule, march).Should().Be(april);
        ScheduleCalculator.FindAfter(schedule, april).Should().BeNull();
    }

    [Test]
    public void YearlySchedule_UsesFebruary28AndReturnsToLeapDay()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2024, 2, 29), new TimeOnly(9, 0),
            "UTC", RepeatUnit.Year, 1, null);

        ScheduleCalculator.FindAfter(schedule, Utc(2024, 2, 29, 9))
            .Should().Be(Utc(2025, 2, 28, 9));
        ScheduleCalculator.FindAfter(schedule, Utc(2027, 2, 28, 9))
            .Should().Be(Utc(2028, 2, 29, 9));
    }

    [Test]
    public void WeeklySchedule_RespectsRepeatInterval()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2026, 11, 2), new TimeOnly(9, 0),
            "UTC", RepeatUnit.Week, 2, null);

        ScheduleCalculator.FindAfter(schedule, Utc(2026, 11, 2, 9))
            .Should().Be(Utc(2026, 11, 16, 9));
        ScheduleCalculator.FindOnOrAfter(schedule, Utc(2026, 11, 17, 9))
            .Should().Be(Utc(2026, 11, 30, 9));
    }

    [Test]
    public void FindOnOrAfter_AfterPause_SkipsEarlierOccurrences()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2026, 11, 13), new TimeOnly(9, 0),
            "UTC", RepeatUnit.Day, 1, null);
        var resumedAt = Utc(2026, 11, 15, 10);

        ScheduleCalculator.FindOnOrAfter(schedule, resumedAt)
            .Should().Be(Utc(2026, 11, 16, 9));
    }

    [Test]
    public void FindAfter_FromLastScheduledOccurrence_ReturnsMissedDatesInOrder()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2026, 11, 13), new TimeOnly(9, 0),
            "UTC", RepeatUnit.Day, 1, new DateOnly(2026, 11, 15));
        var thirteenth = Utc(2026, 11, 13, 9);
        var fourteenth = Utc(2026, 11, 14, 9);
        var fifteenth = Utc(2026, 11, 15, 9);

        ScheduleCalculator.FindAfter(schedule, thirteenth).Should().Be(fourteenth);
        ScheduleCalculator.FindAfter(schedule, fourteenth).Should().Be(fifteenth);
        ScheduleCalculator.FindAfter(schedule, fifteenth).Should().BeNull();
    }

    [Test]
    public void SpringForward_ShiftsOccurrenceByGap_ThenRestoresLocalTime()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2025, 3, 30), new TimeOnly(2, 30),
            "Europe/Berlin", RepeatUnit.Day, 1, null);

        // 02:30 пропущено. Срабатывание переносится на 03:30 по Берлину.
        var shifted = Utc(2025, 3, 30, 1, 30);

        ScheduleCalculator.FindOnOrAfter(schedule, Utc(2025, 3, 30, 0))
            .Should().Be(shifted);

        // На следующий день срабатывание снова в 02:30 по Берлину.
        ScheduleCalculator.FindAfter(schedule, shifted)
            .Should().Be(Utc(2025, 3, 31, 0, 30));
    }

    [Test]
    public void SpringForward_HalfHourGap_ShiftsOccurrenceByThirtyMinutes()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2025, 10, 5), new TimeOnly(2, 15),
            "Australia/Lord_Howe", RepeatUnit.None, null, null);

        // 02:15 переносится на 02:45 по местному времени.
        ScheduleCalculator.FindOnOrAfter(schedule, Utc(2025, 10, 4, 0))
            .Should().Be(Utc(2025, 10, 4, 15, 45));
    }

    [Test]
    public void FallBack_OneTimeSchedule_ReturnsBothUtcInstantsInOrder()
    {
        var date = new DateOnly(2025, 10, 26);
        var schedule = new ReminderSchedule(
            date, new TimeOnly(2, 30),
            "Europe/Berlin", RepeatUnit.None, null, date);
        var first = Utc(2025, 10, 26, 0, 30);
        var second = Utc(2025, 10, 26, 1, 30);

        ScheduleCalculator.FindOnOrAfter(schedule, first).Should().Be(first);
        ScheduleCalculator.FindAfter(schedule, first).Should().Be(second);
        ScheduleCalculator.FindOnOrAfter(schedule, first.AddMinutes(15))
            .Should().Be(second);
        ScheduleCalculator.FindAfter(schedule, second).Should().BeNull();
    }

    [Test]
    public void EndLocalDate_IncludesOccurrenceShiftedIntoNextLocalDay()
    {
        var date = new DateOnly(2011, 12, 30);
        var schedule = new ReminderSchedule(
            date, new TimeOnly(14, 0),
            "Pacific/Apia", RepeatUnit.None, null, date);

        // В Апиа пропустили 30 декабря. Срабатывание переносится
        // на 31 декабря, 14:00 по местному времени.
        ScheduleCalculator.FindOnOrAfter(schedule, Utc(2011, 12, 30, 12))
            .Should().Be(Utc(2011, 12, 31, 0));
    }

    [Test]
    public void SkippedLocalDay_ReturnsEachDistinctUtcInstantOnce()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2011, 12, 30),
            new TimeOnly(14, 0),
            "Pacific/Apia",
            RepeatUnit.Day,
            1,
            new DateOnly(2011, 12, 31));

        var zone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
        var firstUtc = Utc(2011, 12, 31, 0);
        var firstLocal = TimeZoneInfo.ConvertTimeFromUtc(firstUtc.UtcDateTime, zone);

        ScheduleCalculator.FindOnOrAfter(schedule, Utc(2011, 12, 30, 12))
            .Should().Be(firstUtc);

        var next = ScheduleCalculator.FindAfter(schedule, firstUtc);

        switch (firstLocal)
        {
            case var local when local == new DateTime(2011, 12, 31, 14, 0, 0):
                // Пропущено 30 декабря. Обе даты дают один UTC-момент.
                next.Should().BeNull();
                break;

            case var local when local == new DateTime(2011, 12, 30, 14, 0, 0):
                // Пропущено 31 декабря. Сдвинутое срабатывание даёт второй момент.
                var secondUtc = Utc(2012, 1, 1, 0);
                next.Should().Be(secondUtc);
                ScheduleCalculator.FindAfter(schedule, secondUtc).Should().BeNull();
                break;

            default:
                Assert.Fail($"Неожиданное локальное время для Pacific/Apia: {firstLocal:O}");
                break;
        }
    }

    [Test]
    public void Search_BoundaryWithOffset_UsesUtcDate()
    {
        var schedule = new ReminderSchedule(
            new DateOnly(2026, 11, 5), new TimeOnly(23, 0),
            "Etc/GMT+12", RepeatUnit.None, null, null);

        var occurrence = Utc(2026, 11, 6, 11);
        var boundary = occurrence.ToOffset(TimeSpan.FromHours(14));

        ScheduleCalculator.FindOnOrAfter(schedule, boundary)
            .Should().Be(occurrence);
        ScheduleCalculator.FindAfter(schedule, boundary)
            .Should().BeNull();
    }

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute = 0)
    {
        return new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
    }
}

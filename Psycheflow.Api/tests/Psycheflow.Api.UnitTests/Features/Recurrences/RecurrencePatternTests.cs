using Psycheflow.Api.Features.Recurrences;

namespace Psycheflow.Api.UnitTests.Features.Recurrences;

public sealed class RecurrencePatternTests
{
    [Fact]
    public void Weekly_RepeatsOnTheSameWeekday_WithinTheWindow()
    {
        IReadOnlyList<DateOnly> dates = RecurrencePattern.Occurrences(
            RecurrenceType.Weekly, anchor: new DateOnly(2026, 10, 6), from: new DateOnly(2026, 10, 6), until: new DateOnly(2026, 10, 27));

        dates.ShouldBe([new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 27)]);
    }

    [Fact]
    public void Weekly_FromAfterTheAnchor_ContinuesTheSameCadence()
    {
        IReadOnlyList<DateOnly> dates = RecurrencePattern.Occurrences(
            RecurrenceType.Weekly, anchor: new DateOnly(2026, 10, 6), from: new DateOnly(2026, 10, 8), until: new DateOnly(2026, 10, 20));

        dates.ShouldBe([new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 20)]);
    }

    [Fact]
    public void Monthly_KeepsTheDayOfMonth()
    {
        IReadOnlyList<DateOnly> dates = RecurrencePattern.Occurrences(
            RecurrenceType.Monthly, anchor: new DateOnly(2026, 10, 15), from: new DateOnly(2026, 10, 15), until: new DateOnly(2027, 1, 14));

        dates.ShouldBe([new DateOnly(2026, 10, 15), new DateOnly(2026, 11, 15), new DateOnly(2026, 12, 15)]);
    }

    [Fact]
    public void Monthly_OnDay31_UsesTheLastDayOfShorterMonths()
    {
        IReadOnlyList<DateOnly> dates = RecurrencePattern.Occurrences(
            RecurrenceType.Monthly, anchor: new DateOnly(2026, 12, 31), from: new DateOnly(2026, 12, 31), until: new DateOnly(2027, 3, 31));

        dates.ShouldBe([new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 31)]);
    }

    [Fact]
    public void Window_Is3MonthsMinusOneDay_CappedByTheEndDate()
    {
        RecurrencePattern.WindowEnd(new DateOnly(2026, 10, 6), endDate: null).ShouldBe(new DateOnly(2027, 1, 5));
        RecurrencePattern.WindowEnd(new DateOnly(2026, 10, 6), endDate: new DateOnly(2026, 11, 1)).ShouldBe(new DateOnly(2026, 11, 1));
    }
}

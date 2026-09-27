using System.Globalization;
using Psycheflow.Api.Features.Dashboard;

namespace Psycheflow.Api.UnitTests.Features.Dashboard;

public sealed class DashboardPeriodTests
{
    [Theory]
    [InlineData("2026-10-05", "2026-10-05", "2026-10-11")] // segunda-feira
    [InlineData("2026-10-08", "2026-10-05", "2026-10-11")] // quinta-feira
    [InlineData("2026-10-11", "2026-10-05", "2026-10-11")] // domingo fecha a semana
    public void For_WeekStartsOnMonday(string today, string weekStart, string weekEnd)
    {
        DashboardPeriod period = DashboardPeriod.For(DateOnly.Parse(today, CultureInfo.InvariantCulture));

        period.WeekStart.ShouldBe(DateOnly.Parse(weekStart, CultureInfo.InvariantCulture));
        period.WeekEnd.ShouldBe(DateOnly.Parse(weekEnd, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void For_MonthCoversTheWholeCalendarMonth()
    {
        DashboardPeriod period = DashboardPeriod.For(new DateOnly(2026, 2, 14));

        period.MonthStart.ShouldBe(new DateOnly(2026, 2, 1));
        period.MonthEnd.ShouldBe(new DateOnly(2026, 2, 28));
    }
}

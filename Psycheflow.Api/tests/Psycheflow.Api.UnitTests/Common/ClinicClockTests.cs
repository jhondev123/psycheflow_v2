using Microsoft.Extensions.Time.Testing;
using Psycheflow.Api.Common.Time;

namespace Psycheflow.Api.UnitTests.Common;

public sealed class ClinicClockTests
{
    [Fact]
    public void LocalNow_ConvertsUtcToClinicTimeZone()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));
        var clock = new ClinicClock(time);

        DateTime local = clock.LocalNow(ClinicClock.DefaultTimeZone);

        local.ShouldBe(new DateTime(2026, 10, 5, 9, 0, 0));
    }

    [Fact]
    public void Today_NearMidnightUtc_UsesClinicDate()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 6, 1, 30, 0, TimeSpan.Zero));
        var clock = new ClinicClock(time);

        clock.Today(ClinicClock.DefaultTimeZone).ShouldBe(new DateOnly(2026, 10, 5));
    }

    [Theory]
    [InlineData("America/Sao_Paulo", true)]
    [InlineData("America/Manaus", true)]
    [InlineData("Mars/Olympus", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidTimeZone_AcceptsOnlyKnownIanaIds(string? timeZone, bool expected) =>
        ClinicClock.IsValidTimeZone(timeZone).ShouldBe(expected);
}

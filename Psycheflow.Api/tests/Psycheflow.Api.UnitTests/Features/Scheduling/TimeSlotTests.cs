using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.UnitTests.Features.Scheduling;

public sealed class TimeSlotTests
{
    private static readonly DateOnly Day = new(2026, 10, 6);

    private static TimeSlot Slot(int startHour, int startMinute, int endHour, int endMinute) =>
        TimeSlot.Create(Day, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute)).Value;

    [Fact]
    public void FromDuration_ComputesEnd()
    {
        TimeSlot slot = TimeSlot.FromDuration(Day, new TimeOnly(14, 0), 50).Value;

        slot.End.ShouldBe(new TimeOnly(14, 50));
        slot.DurationMinutes.ShouldBe(50);
    }

    [Fact]
    public void FromDuration_CrossingMidnight_Fails() =>
        TimeSlot.FromDuration(Day, new TimeOnly(23, 30), 50).Error.ShouldBe(SchedulingErrors.CrossesMidnight);

    [Theory]
    [InlineData(14, 0, 14, 0)]
    [InlineData(15, 0, 14, 0)]
    public void Create_EndNotAfterStart_Fails(int startHour, int startMinute, int endHour, int endMinute) =>
        TimeSlot.Create(Day, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute))
            .Error.ShouldBe(SchedulingErrors.InvalidRange);

    [Theory]
    [InlineData(14, 0, 15, 0, true)]
    [InlineData(14, 30, 15, 30, true)]
    [InlineData(13, 30, 14, 10, true)]
    [InlineData(13, 0, 16, 0, true)]
    [InlineData(15, 0, 16, 0, false)]
    [InlineData(13, 0, 14, 0, false)]
    public void Overlaps_SameDay(int startHour, int startMinute, int endHour, int endMinute, bool expected) =>
        Slot(14, 0, 15, 0).Overlaps(Slot(startHour, startMinute, endHour, endMinute)).ShouldBe(expected);

    [Fact]
    public void Overlaps_DifferentDays_IsFalse()
    {
        TimeSlot other = TimeSlot.Create(Day.AddDays(1), new TimeOnly(14, 0), new TimeOnly(15, 0)).Value;

        Slot(14, 0, 15, 0).Overlaps(other).ShouldBeFalse();
    }

    [Fact]
    public void WholeDay_CoversTheDay() =>
        TimeSlot.WholeDay(Day).ShouldBe(TimeSlot.Create(Day, TimeOnly.MinValue, new TimeOnly(23, 59)).Value);

    [Fact]
    public void StartsBefore_And_EndsBefore_UseDateAndTime()
    {
        TimeSlot slot = Slot(14, 0, 15, 0);

        slot.StartsBefore(new DateTime(2026, 10, 6, 14, 30, 0)).ShouldBeTrue();
        slot.StartsBefore(new DateTime(2026, 10, 6, 13, 59, 0)).ShouldBeFalse();
        slot.EndsBefore(new DateTime(2026, 10, 6, 14, 30, 0)).ShouldBeFalse();
        slot.EndsBefore(new DateTime(2026, 10, 7, 8, 0, 0)).ShouldBeTrue();
    }

    [Fact]
    public void Errors_AreValidationErrors() =>
        SchedulingErrors.InvalidRange.Type.ShouldBe(ErrorType.Validation);
}

using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.UnitTests.Features.Psychologists;

public sealed class PsychologistTests
{
    private static Psychologist NewPsychologist() => Psychologist.Create(
        Guid.CreateVersion7(), Guid.CreateVersion7(), LicenseNumber.Create("06/12345").Value, ApproachType.Behavioral, phone: null);

    private static WorkingHoursRange Range(DayOfWeek day, int startHour, int endHour) =>
        new(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    [Fact]
    public void SetWorkingHours_ValidRanges_ReplacesPreviousOnesOrdered()
    {
        Psychologist psychologist = NewPsychologist();
        psychologist.SetWorkingHours([Range(DayOfWeek.Friday, 8, 12)]);

        Result result = psychologist.SetWorkingHours(
        [
            Range(DayOfWeek.Tuesday, 8, 12),
            Range(DayOfWeek.Monday, 13, 18),
            Range(DayOfWeek.Monday, 8, 12),
        ]);

        result.IsSuccess.ShouldBeTrue();
        psychologist.WorkingHours.Select(h => (h.DayOfWeek, h.StartTime.Hour)).ShouldBe(
        [
            (DayOfWeek.Monday, 8),
            (DayOfWeek.Monday, 13),
            (DayOfWeek.Tuesday, 8),
        ]);
    }

    [Fact]
    public void SetWorkingHours_RangesTouchingAtTheEdge_AreAllowed() =>
        NewPsychologist().SetWorkingHours([Range(DayOfWeek.Monday, 8, 12), Range(DayOfWeek.Monday, 12, 18)]).IsSuccess.ShouldBeTrue();

    [Fact]
    public void SetWorkingHours_OverlappingRanges_FailsAndKeepsPreviousHours()
    {
        Psychologist psychologist = NewPsychologist();
        psychologist.SetWorkingHours([Range(DayOfWeek.Friday, 8, 12)]);

        Result result = psychologist.SetWorkingHours([Range(DayOfWeek.Monday, 8, 12), Range(DayOfWeek.Monday, 11, 14)]);

        result.Error.ShouldBe(PsychologistErrors.OverlappingWorkingHours);
        psychologist.WorkingHours.Single().DayOfWeek.ShouldBe(DayOfWeek.Friday);
    }

    [Fact]
    public void SetWorkingHours_SameRangeOnDifferentDays_IsAllowed() =>
        NewPsychologist().SetWorkingHours([Range(DayOfWeek.Monday, 8, 12), Range(DayOfWeek.Tuesday, 8, 12)]).IsSuccess.ShouldBeTrue();

    [Fact]
    public void SetWorkingHours_EndNotAfterStart_Fails() =>
        NewPsychologist().SetWorkingHours([Range(DayOfWeek.Monday, 12, 12)]).Error.ShouldBe(PsychologistErrors.InvalidWorkingHoursRange);

    [Theory]
    [InlineData(8, 0, 9, 0, true)]
    [InlineData(11, 10, 12, 0, true)]
    [InlineData(11, 30, 12, 20, false)]
    [InlineData(12, 30, 13, 20, false)]
    [InlineData(7, 30, 8, 20, false)]
    public void WorksAt_IntervalMustFitInsideOneRange(int startHour, int startMinute, int endHour, int endMinute, bool expected)
    {
        Psychologist psychologist = NewPsychologist();
        psychologist.SetWorkingHours([Range(DayOfWeek.Monday, 8, 12), Range(DayOfWeek.Monday, 13, 18)]);

        bool works = psychologist.WorksAt(
            DayOfWeek.Monday, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));

        works.ShouldBe(expected);
    }

    [Fact]
    public void WorksAt_DayWithoutHours_IsFalse()
    {
        Psychologist psychologist = NewPsychologist();
        psychologist.SetWorkingHours([Range(DayOfWeek.Monday, 8, 12)]);

        psychologist.WorksAt(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(10, 0)).ShouldBeFalse();
    }
}

using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.UnitTests.Features.Sessions;

public sealed class SessionTests
{
    private static readonly TimeSlot Slot = TimeSlot.Create(new DateOnly(2026, 10, 6), new TimeOnly(14, 0), new TimeOnly(14, 50)).Value;
    private static readonly DateTime BeforeStart = new(2026, 10, 6, 13, 0, 0);
    private static readonly DateTime AfterStart = new(2026, 10, 6, 14, 10, 0);

    private static Session NewSession() => Session.Book(Guid.CreateVersion7(), Guid.CreateVersion7(), Slot, notes: null);

    [Fact]
    public void Book_CreatesScheduledSessionWithPendingSchedule()
    {
        Session session = NewSession();

        session.Status.ShouldBe(SessionStatus.Scheduled);
        session.Schedule.Type.ShouldBe(ScheduleType.Session);
        session.Schedule.Status.ShouldBe(ScheduleStatus.Pending);
        session.Schedule.Slot.ShouldBe(Slot);
        session.Schedule.PsychologistId.ShouldBe(session.PsychologistId);
    }

    [Fact]
    public void Cancel_SetsSessionAndScheduleCancelled()
    {
        Session session = NewSession();

        Result result = session.Cancel("  Paciente doente  ");

        result.IsSuccess.ShouldBeTrue();
        session.Status.ShouldBe(SessionStatus.Cancelled);
        session.Schedule.Status.ShouldBe(ScheduleStatus.Cancelled);
        session.CancellationReason.ShouldBe("Paciente doente");
    }

    [Fact]
    public void Complete_AfterStart_WithNotesAndScore_Succeeds()
    {
        Session session = NewSession();

        Result result = session.Complete("Evolução", 10, "ótima", AfterStart);

        result.IsSuccess.ShouldBeTrue();
        session.Status.ShouldBe(SessionStatus.Completed);
        session.FeedbackScore.ShouldBe(10);
        session.Notes.ShouldBe("Evolução");
    }

    [Fact]
    public void Complete_BeforeStart_Fails() =>
        NewSession().Complete("Evolução", 5, null, BeforeStart).Error.ShouldBe(SessionErrors.NotStartedYet);

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Complete_ScoreOutOfRange_Fails(int score) =>
        NewSession().Complete("Evolução", score, null, AfterStart).Error.ShouldBe(SessionErrors.InvalidFeedbackScore);

    [Fact]
    public void Complete_WithoutNotes_Fails() =>
        NewSession().Complete("  ", 5, null, AfterStart).Error.ShouldBe(SessionErrors.NotesRequired);

    [Fact]
    public void MarkNoShow_OnlyAfterStart()
    {
        Session session = NewSession();

        session.MarkNoShow(BeforeStart).Error.ShouldBe(SessionErrors.NotStartedYet);
        session.MarkNoShow(AfterStart).IsSuccess.ShouldBeTrue();
        session.Status.ShouldBe(SessionStatus.NoShow);
    }

    [Fact]
    public void Reschedule_MovesScheduleAndRecordsReason()
    {
        Session session = NewSession();
        TimeSlot newSlot = TimeSlot.Create(new DateOnly(2026, 10, 7), new TimeOnly(9, 0), new TimeOnly(9, 50)).Value;
        session.Confirm();

        Result result = session.Reschedule(newSlot, "Feriado");

        result.IsSuccess.ShouldBeTrue();
        session.Schedule.Slot.ShouldBe(newSlot);
        session.Schedule.Status.ShouldBe(ScheduleStatus.Pending);
        session.RescheduleReason.ShouldBe("Feriado");
    }

    [Theory]
    [InlineData(SessionStatus.Completed)]
    [InlineData(SessionStatus.Cancelled)]
    [InlineData(SessionStatus.NoShow)]
    public void ClosedSession_RejectsEveryChange(SessionStatus closedStatus)
    {
        Session session = NewSession();
        Close(session, closedStatus);

        session.Cancel("x").Error.ShouldBe(SessionErrors.NotOpen);
        session.Reschedule(Slot, "x").Error.ShouldBe(SessionErrors.NotOpen);
        session.Complete("x", 5, null, AfterStart).Error.ShouldBe(SessionErrors.NotOpen);
        session.MarkNoShow(AfterStart).Error.ShouldBe(SessionErrors.NotOpen);
        session.Confirm().Error.ShouldBe(SessionErrors.NotOpen);
        session.UpdateDetails(Guid.CreateVersion7(), "x").Error.ShouldBe(SessionErrors.NotOpen);
    }

    [Fact]
    public void UpdateDetails_NullNotes_KeepsCurrentNotes()
    {
        Session session = NewSession();
        session.UpdateDetails(session.PatientId, "Plano da sessão");
        Guid otherPatient = Guid.CreateVersion7();

        session.UpdateDetails(otherPatient, notes: null);

        session.PatientId.ShouldBe(otherPatient);
        session.Notes.ShouldBe("Plano da sessão");
    }

    private static void Close(Session session, SessionStatus status)
    {
        Result result = status switch
        {
            SessionStatus.Completed => session.Complete("ok", 7, null, AfterStart),
            SessionStatus.Cancelled => session.Cancel("motivo"),
            SessionStatus.NoShow => session.MarkNoShow(AfterStart),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
        result.IsSuccess.ShouldBeTrue();
    }
}

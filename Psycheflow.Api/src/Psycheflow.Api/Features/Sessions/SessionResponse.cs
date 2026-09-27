using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.Features.Sessions;

/// <param name="ClinicalNotesVisible">
/// Falso quando o usuário não é o psicólogo da sessão: anotações e feedback vêm nulos (sigilo, D-02).
/// </param>
public sealed record SessionResponse(
    Guid Id,
    Guid ScheduleId,
    Guid PsychologistId,
    string PsychologistName,
    Guid PatientId,
    string PatientName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    SessionStatus Status,
    ScheduleStatus ScheduleStatus,
    string? Notes,
    int? FeedbackScore,
    string? FeedbackComment,
    string? CancellationReason,
    string? RescheduleReason,
    bool ClinicalNotesVisible,
    Guid? RecurrenceId,
    SessionPaymentSummary? Payment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    /// <summary>Requer <c>Schedule</c>, <c>Patient</c>, <c>Payment</c> e <c>Psychologist.User</c> carregados.</summary>
    public static SessionResponse From(Session session, bool clinicalNotesVisible) => new(
        session.Id,
        session.ScheduleId,
        session.PsychologistId,
        session.Psychologist!.User!.FullName,
        session.PatientId,
        session.Patient!.FullName,
        session.Schedule.Date,
        session.Schedule.StartTime,
        session.Schedule.EndTime,
        session.Schedule.Slot.DurationMinutes,
        session.Status,
        session.Schedule.Status,
        clinicalNotesVisible ? session.Notes : null,
        clinicalNotesVisible ? session.FeedbackScore : null,
        clinicalNotesVisible ? session.FeedbackComment : null,
        session.CancellationReason,
        session.RescheduleReason,
        clinicalNotesVisible,
        session.RecurrenceId,
        SessionPaymentSummary.From(session.Payment),
        session.CreatedAt,
        session.UpdatedAt);
}

public sealed record SessionListItem(
    Guid Id,
    Guid PsychologistId,
    string PsychologistName,
    Guid PatientId,
    string PatientName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    SessionStatus Status,
    ScheduleStatus ScheduleStatus);

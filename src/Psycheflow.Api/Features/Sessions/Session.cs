using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.Features.Sessions;

/// <summary>
/// Atendimento de um paciente. É a raiz do agregado: controla o próprio <see cref="Schedule"/> (RN-36)
/// e só aceita alterações enquanto está <see cref="SessionStatus.Scheduled"/> (RN-41).
/// </summary>
public sealed class Session : Entity, ITenantEntity, ISoftDeletable
{
    public const int NotesMaxLength = 20_000;
    public const int CommentMaxLength = 1000;
    public const int ReasonMaxLength = 500;
    public const int MinFeedbackScore = 0;
    public const int MaxFeedbackScore = 10;

    private Session()
    {
    }

    public Guid CompanyId { get; private set; }

    public Guid ScheduleId { get; private set; }

    public Schedule Schedule { get; private set; } = null!;

    public Guid PsychologistId { get; private set; }

    public Psychologist? Psychologist { get; private set; }

    public Guid PatientId { get; private set; }

    public Patient? Patient { get; private set; }

    public SessionStatus Status { get; private set; }

    /// <summary>Anotações em Markdown (RN-46). Sigilosas: só o psicólogo da sessão vê.</summary>
    public string? Notes { get; private set; }

    /// <summary>Nota de 0 a 10 dada na conclusão (RN-45).</summary>
    public int? FeedbackScore { get; private set; }

    public string? FeedbackComment { get; private set; }

    public string? CancellationReason { get; private set; }

    /// <summary>Motivo do último reagendamento (RN-42).</summary>
    public string? RescheduleReason { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsOpen => Status == SessionStatus.Scheduled;

    public static Session Book(Guid psychologistId, Guid patientId, TimeSlot slot, string? notes)
    {
        var schedule = Schedule.ForSession(psychologistId, slot);
        return new Session
        {
            Schedule = schedule,
            ScheduleId = schedule.Id,
            PsychologistId = psychologistId,
            PatientId = patientId,
            Status = SessionStatus.Scheduled,
            Notes = Normalize(notes),
        };
    }

    public Result Confirm()
    {
        if (!IsOpen)
        {
            return SessionErrors.NotOpen;
        }

        Schedule.Confirm();
        return Result.Success();
    }

    /// <summary>RN-42: novo horário + motivo. As regras de disponibilidade são checadas antes, pelo caso de uso.</summary>
    public Result Reschedule(TimeSlot slot, string reason)
    {
        if (!IsOpen)
        {
            return SessionErrors.NotOpen;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return SessionErrors.ReasonRequired;
        }

        Schedule.MoveTo(slot);
        RescheduleReason = reason.Trim();
        return Result.Success();
    }

    /// <summary>RN-43: cancelar exige motivo e libera o horário.</summary>
    public Result Cancel(string reason)
    {
        if (!IsOpen)
        {
            return SessionErrors.NotOpen;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return SessionErrors.ReasonRequired;
        }

        Status = SessionStatus.Cancelled;
        CancellationReason = reason.Trim();
        Schedule.Cancel();
        return Result.Success();
    }

    /// <summary>RN-45: concluir exige anotações e nota de 0 a 10, e só depois do início da sessão.</summary>
    public Result Complete(string notes, int feedbackScore, string? feedbackComment, DateTime localNow)
    {
        if (!IsOpen)
        {
            return SessionErrors.NotOpen;
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            return SessionErrors.NotesRequired;
        }

        if (feedbackScore is < MinFeedbackScore or > MaxFeedbackScore)
        {
            return SessionErrors.InvalidFeedbackScore;
        }

        if (!Schedule.Slot.StartsBefore(localNow))
        {
            return SessionErrors.NotStartedYet;
        }

        Status = SessionStatus.Completed;
        Notes = notes.Trim();
        FeedbackScore = feedbackScore;
        FeedbackComment = Normalize(feedbackComment);
        return Result.Success();
    }

    /// <summary>RN-47: falta do paciente, só depois do horário de início.</summary>
    public Result MarkNoShow(DateTime localNow)
    {
        if (!IsOpen)
        {
            return SessionErrors.NotOpen;
        }

        if (!Schedule.Slot.StartsBefore(localNow))
        {
            return SessionErrors.NotStartedYet;
        }

        Status = SessionStatus.NoShow;
        return Result.Success();
    }

    /// <summary>RF008: troca de paciente e anotações prévias. <paramref name="notes"/> nulo mantém as atuais.</summary>
    public Result UpdateDetails(Guid patientId, string? notes)
    {
        if (!IsOpen)
        {
            return SessionErrors.NotOpen;
        }

        PatientId = patientId;
        if (notes is not null)
        {
            Notes = Normalize(notes);
        }

        return Result.Success();
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

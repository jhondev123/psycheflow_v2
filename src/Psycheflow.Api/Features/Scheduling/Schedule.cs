using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Scheduling;

/// <summary>
/// Item da agenda de um psicólogo: um atendimento (criado junto com a <c>Session</c>) ou um bloqueio.
/// Data e horários ficam no horário local da clínica.
/// </summary>
public sealed class Schedule : Entity, ITenantEntity, ISoftDeletable
{
    public const int ReasonMaxLength = 500;

    private Schedule()
    {
    }

    public Guid CompanyId { get; private set; }

    public Guid PsychologistId { get; private set; }

    public DateOnly Date { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public ScheduleType Type { get; private set; }

    public ScheduleStatus Status { get; private set; }

    /// <summary>Motivo do bloqueio (opcional, RN-39).</summary>
    public string? BlockReason { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public TimeSlot Slot => TimeSlot.FromStorage(Date, StartTime, EndTime);

    internal static Schedule ForSession(Guid psychologistId, TimeSlot slot) =>
        New(psychologistId, slot, ScheduleType.Session, ScheduleStatus.Pending, reason: null);

    public static Schedule Block(Guid psychologistId, TimeSlot slot, string? reason) =>
        New(psychologistId, slot, ScheduleType.Block, ScheduleStatus.Confirmed, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim());

    internal void Confirm() => Status = ScheduleStatus.Confirmed;

    internal void Cancel() => Status = ScheduleStatus.Cancelled;

    /// <summary>Reagendar volta o item para pendente (precisa ser reconfirmado).</summary>
    internal void MoveTo(TimeSlot slot)
    {
        SetSlot(slot);
        Status = ScheduleStatus.Pending;
    }

    private static Schedule New(Guid psychologistId, TimeSlot slot, ScheduleType type, ScheduleStatus status, string? reason)
    {
        var schedule = new Schedule
        {
            PsychologistId = psychologistId,
            Type = type,
            Status = status,
            BlockReason = reason,
        };
        schedule.SetSlot(slot);
        return schedule;
    }

    private void SetSlot(TimeSlot slot)
    {
        Date = slot.Date;
        StartTime = slot.Start;
        EndTime = slot.End;
    }
}

using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Scheduling;

/// <summary>
/// Regras de disponibilidade da agenda, compartilhadas por sessões e bloqueios:
/// RN-30 (não no passado), RN-33 (dentro do expediente, só sessões) e RN-34/RN-38 (sem sobreposição).
/// </summary>
public sealed class ScheduleAvailability(AppDbContext db, ICurrentUser currentUser)
{
    /// <summary>
    /// Serializa escritas na agenda de um psicólogo até o fim da transação atual (advisory lock do Postgres),
    /// evitando que duas requisições simultâneas passem pela checagem de conflito e marquem o mesmo horário.
    /// </summary>
    public Task LockAgendaAsync(Guid psychologistId, CancellationToken cancellationToken)
    {
        long key = BitConverter.ToInt64(psychologistId.ToByteArray(), 0);
        return db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    public async Task<Result> CheckSessionSlotAsync(
        Psychologist psychologist, TimeSlot slot, DateTime localNow, Guid? ignoreScheduleId, CancellationToken cancellationToken)
    {
        if (slot.StartsBefore(localNow))
        {
            return SchedulingErrors.InThePast;
        }

        if (!psychologist.WorksAt(slot.Date.DayOfWeek, slot.Start, slot.End))
        {
            return SchedulingErrors.OutsideWorkingHours;
        }

        return await HasConflictAsync(psychologist.Id, slot, ignoreScheduleId, cancellationToken)
            ? SchedulingErrors.Conflict
            : Result.Success();
    }

    /// <summary>RN-40: bloqueios não precisam respeitar o expediente; só não podem terminar no passado nem conflitar.</summary>
    public async Task<Result> CheckBlockSlotAsync(Guid psychologistId, TimeSlot slot, DateTime localNow, CancellationToken cancellationToken)
    {
        if (slot.EndsBefore(localNow))
        {
            return SchedulingErrors.InThePast;
        }

        return await HasConflictAsync(psychologistId, slot, ignoreScheduleId: null, cancellationToken)
            ? SchedulingErrors.Conflict
            : Result.Success();
    }

    /// <summary>
    /// Psicólogo cuja agenda será alterada: o informado (Admin/Manager ou o próprio) ou, se omitido, o do usuário logado.
    /// </summary>
    public async Task<Result<Psychologist>> ResolvePsychologistAsync(Guid? requestedId, CancellationToken cancellationToken)
    {
        if ((requestedId ?? currentUser.PsychologistId) is not { } psychologistId)
        {
            return SchedulingErrors.PsychologistRequired;
        }

        Psychologist? psychologist = await db.FindPsychologistAsync(psychologistId, cancellationToken);
        if (psychologist is null)
        {
            return PsychologistErrors.NotFound;
        }

        return currentUser.CanManagePsychologist(psychologist.Id) ? psychologist : SchedulingErrors.CannotManageAgenda;
    }

    private Task<bool> HasConflictAsync(Guid psychologistId, TimeSlot slot, Guid? ignoreScheduleId, CancellationToken cancellationToken) =>
        db.Schedules.AnyAsync(
            s => s.PsychologistId == psychologistId
                && s.Date == slot.Date
                && s.Status != ScheduleStatus.Cancelled
                && s.Id != ignoreScheduleId
                && s.StartTime < slot.End
                && slot.Start < s.EndTime,
            cancellationToken);
}

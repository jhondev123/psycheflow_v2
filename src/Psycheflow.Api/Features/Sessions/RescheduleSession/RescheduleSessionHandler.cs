using Microsoft.EntityFrameworkCore.Storage;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.Features.Sessions.RescheduleSession;

/// <summary>UC06 / RF006: novo horário com motivo, reaplicando RN-30/33/34 (ignorando o próprio horário atual).</summary>
public sealed class RescheduleSessionHandler(AppDbContext db, ScheduleAvailability availability, SessionAccess access)
{
    public async Task<Result<SessionResponse>> Handle(Guid id, RescheduleSessionRequest request, CancellationToken cancellationToken)
    {
        Result<Session> found = await access.FindManageableAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        Session session = found.Value;
        if (!session.IsOpen)
        {
            return SessionErrors.NotOpen;
        }

        (_, DateTime localNow) = await access.GetClinicContextAsync(cancellationToken);
        Result<TimeSlot> slot = SessionAccess.BuildSlot(
            request.Date!.Value, request.StartTime!.Value, request.DurationMinutes, session.Schedule.Slot.DurationMinutes);
        if (slot.IsFailure)
        {
            return slot.Error;
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await availability.LockAgendaAsync(session.PsychologistId, cancellationToken);

        Result available = await availability.CheckSessionSlotAsync(
            session.Psychologist!, slot.Value, localNow, ignoreScheduleId: session.ScheduleId, cancellationToken);
        if (available.IsFailure)
        {
            return available.Error;
        }

        Result rescheduled = session.Reschedule(slot.Value, request.Reason!);
        if (rescheduled.IsFailure)
        {
            return rescheduled.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return access.ToResponse(session);
    }
}

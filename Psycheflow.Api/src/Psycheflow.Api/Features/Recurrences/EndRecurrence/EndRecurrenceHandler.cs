using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Recurrences.EndRecurrence;

public sealed record EndRecurrenceRequest(string? Reason = null);

/// <summary>
/// Encerra a recorrência e cancela as sessões futuras ainda agendadas (com os pagamentos pendentes).
/// Sessões já realizadas não mudam.
/// </summary>
public sealed class EndRecurrenceHandler(AppDbContext db, RecurrenceAccess access, SessionAccess sessions)
{
    private const string DefaultReason = "Recorrência encerrada";

    public async Task<Result<RecurrenceResponse>> Handle(Guid id, EndRecurrenceRequest request, CancellationToken cancellationToken)
    {
        Result<Recurrence> found = await access.FindManageableAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        Recurrence recurrence = found.Value;
        Result ended = recurrence.End(request.Reason);
        if (ended.IsFailure)
        {
            return ended.Error;
        }

        (_, DateTime localNow) = await sessions.GetClinicContextAsync(cancellationToken);
        List<Session> openSessions = await db.Sessions
            .Include(s => s.Schedule)
            .Include(s => s.Payment)
            .Where(s => s.RecurrenceId == recurrence.Id && s.Status == SessionStatus.Scheduled)
            .ToListAsync(cancellationToken);

        string reason = recurrence.EndReason is null ? DefaultReason : $"{DefaultReason}: {recurrence.EndReason}";
        foreach (Session session in openSessions.Where(s => !s.Schedule.Slot.StartsBefore(localNow)))
        {
            Result cancelled = SessionAccess.Cancel(session, reason);
            if (cancelled.IsFailure)
            {
                return cancelled.Error;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return RecurrenceResponse.From(recurrence);
    }
}

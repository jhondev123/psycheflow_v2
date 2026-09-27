using Microsoft.EntityFrameworkCore.Storage;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Recurrences;

/// <summary>
/// Gera as sessões de uma janela da recorrência aplicando as mesmas regras de um agendamento avulso (RN-30/33/34).
/// Datas que falham numa regra são puladas e devolvidas com o motivo (D-05); cada sessão criada nasce com pagamento
/// pendente no valor da recorrência (RN-50/51).
/// </summary>
public sealed class RecurrenceGenerator(AppDbContext db, ScheduleAvailability availability, SessionAccess sessions)
{
    public async Task<(IReadOnlyList<DateOnly> Scheduled, IReadOnlyList<SkippedOccurrence> Skipped)> GenerateNextWindowAsync(
        Recurrence recurrence, Psychologist psychologist, DateTime localNow, CancellationToken cancellationToken)
    {
        DateOnly from = recurrence.NextWindowStart;
        DateOnly until = RecurrencePattern.WindowEnd(from, recurrence.EndDate);

        List<DateOnly> scheduled = [];
        List<SkippedOccurrence> skipped = [];

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await availability.LockAgendaAsync(psychologist.Id, cancellationToken);

        foreach (DateOnly date in RecurrencePattern.Occurrences(recurrence.Type, recurrence.StartDate, from, until))
        {
            Result<TimeSlot> slot = TimeSlot.FromDuration(date, recurrence.StartTime, recurrence.DurationMinutes);
            Result available = slot.IsFailure
                ? slot.Error
                : await availability.CheckSessionSlotAsync(psychologist, slot.Value, localNow, null, cancellationToken);

            if (available.IsFailure)
            {
                skipped.Add(new SkippedOccurrence(date, available.Error.Code, available.Error.Message));
                continue;
            }

            var session = Session.Book(psychologist.Id, recurrence.PatientId, slot.Value, notes: null, recurrence.Id);
            sessions.AddWithPayment(session, recurrence.Price);

            // Salva a cada sessão para que a próxima checagem de conflito já a enxergue.
            await db.SaveChangesAsync(cancellationToken);
            scheduled.Add(date);
        }

        recurrence.MarkGeneratedUntil(until);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (scheduled, skipped);
    }
}

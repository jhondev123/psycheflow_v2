using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Recurrences.ExtendRecurrence;

/// <summary>Gera a próxima janela de 3 meses de uma recorrência ativa (D-05).</summary>
public sealed class ExtendRecurrenceHandler(
    AppDbContext db, RecurrenceAccess access, SessionAccess sessions, RecurrenceGenerator generator)
{
    public async Task<Result<RecurrenceGenerationResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<Recurrence> found = await access.FindManageableAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        Recurrence recurrence = found.Value;
        if (!recurrence.IsActive)
        {
            return RecurrenceErrors.NotActive;
        }

        if (recurrence.IsFullyGenerated)
        {
            return RecurrenceErrors.FullyGenerated;
        }

        Psychologist? psychologist = await db.FindPsychologistAsync(recurrence.PsychologistId, cancellationToken);
        if (psychologist is null)
        {
            return PsychologistErrors.NotFound;
        }

        (_, DateTime localNow) = await sessions.GetClinicContextAsync(cancellationToken);
        (IReadOnlyList<DateOnly> scheduled, IReadOnlyList<SkippedOccurrence> skipped) =
            await generator.GenerateNextWindowAsync(recurrence, psychologist, localNow, cancellationToken);

        return new RecurrenceGenerationResponse(RecurrenceResponse.From(recurrence), scheduled, skipped);
    }
}

using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Recurrences.ListRecurrences;

public sealed record ListRecurrencesQuery(Guid? PatientId = null, bool? ActiveOnly = null);

/// <summary>Recorrências visíveis ao usuário: todas (Admin/Manager) ou só as do próprio psicólogo.</summary>
public sealed class ListRecurrencesHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<RecurrenceResponse>> Handle(ListRecurrencesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Recurrence> recurrences = db.Recurrences.AsNoTracking();

        if (!currentUser.IsManagement())
        {
            Guid? own = currentUser.PsychologistId;
            recurrences = recurrences.Where(r => r.PsychologistId == own);
        }

        if (query.PatientId is { } patientId)
        {
            recurrences = recurrences.Where(r => r.PatientId == patientId);
        }

        if (query.ActiveOnly == true)
        {
            recurrences = recurrences.Where(r => r.IsActive);
        }

        List<Recurrence> list = await recurrences.OrderByDescending(r => r.StartDate).ToListAsync(cancellationToken);
        return [.. list.Select(RecurrenceResponse.From)];
    }
}

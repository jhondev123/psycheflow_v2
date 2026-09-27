using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.Features.Recurrences;

public sealed class RecurrenceAccess(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<Recurrence>> FindManageableAsync(Guid id, CancellationToken cancellationToken)
    {
        Recurrence? recurrence = await db.Recurrences.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recurrence is null)
        {
            return RecurrenceErrors.NotFound;
        }

        return currentUser.CanManagePsychologist(recurrence.PsychologistId) ? recurrence : SchedulingErrors.CannotManageAgenda;
    }
}

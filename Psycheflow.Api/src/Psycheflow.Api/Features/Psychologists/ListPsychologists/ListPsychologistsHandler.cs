using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Psychologists.ListPsychologists;

public sealed class ListPsychologistsHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<PsychologistResponse>> Handle(CancellationToken cancellationToken)
    {
        List<Psychologist> psychologists = await db.Psychologists
            .AsNoTracking()
            .Include(p => p.User)
            .OrderBy(p => p.User!.FullName)
            .ToListAsync(cancellationToken);

        return [.. psychologists.Select(PsychologistResponse.From)];
    }
}

using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Psychologists;

public static class PsychologistQueries
{
    /// <summary>Psicólogo da empresa do usuário logado (filtro global), com o usuário e o expediente carregados.</summary>
    public static Task<Psychologist?> FindPsychologistAsync(this AppDbContext db, Guid id, CancellationToken cancellationToken) =>
        db.Psychologists
            .Include(p => p.User)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
}

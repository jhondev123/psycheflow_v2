using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Companies;

/// <summary>A empresa é a raiz do tenant (não tem filtro automático): sempre acessada pelo id do usuário logado.</summary>
public static class CompanyAccess
{
    public static Task<Company?> FindCurrentAsync(this AppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        db.Companies.SingleOrDefaultAsync(c => c.Id == currentUser.CompanyId, cancellationToken);

    /// <summary>Leitura (sem rastreamento) das configurações da empresa do usuário logado.</summary>
    public static Task<CompanySettings> GetCurrentSettingsAsync(this AppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        db.Companies
            .AsNoTracking()
            .Where(c => c.Id == currentUser.CompanyId)
            .Select(c => c.Settings)
            .SingleAsync(cancellationToken);
}

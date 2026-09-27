using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Auth;

/// <summary>Monta o JWT de um usuário (roles + empresa + perfil de psicólogo). Usado por registro, login e troca de senha.</summary>
public sealed class AccessTokenIssuer(AppDbContext db, UserManager<User> userManager, TokenService tokenService)
{
    public async Task<AuthResponse> IssueAsync(User user, CancellationToken cancellationToken)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);

        // Pode rodar sem usuário autenticado (login/registro): filtra a empresa explicitamente.
        Guid? psychologistId = await db.Psychologists
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .Where(p => p.UserId == user.Id && p.CompanyId == user.CompanyId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        AccessToken token = tokenService.Create(new TokenSubject(
            user.Id,
            user.Email!,
            user.FullName,
            user.CompanyId,
            psychologistId,
            user.MustChangePassword,
            [.. roles]));

        return new AuthResponse(token.Token, token.ExpiresAt, user.MustChangePassword);
    }
}

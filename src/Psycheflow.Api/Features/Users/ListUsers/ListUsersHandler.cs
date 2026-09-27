using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Users.ListUsers;

public sealed class ListUsersHandler(AppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<UserResponse>> Handle(CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        // Usuários não têm o filtro automático de empresa (ver User): o filtro é explícito aqui.
        var users = await db.Users
            .Where(u => u.CompanyId == currentUser.CompanyId)
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.MustChangePassword,
                u.LockoutEnd,
                Roles = db.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
                    .ToList(),
                PsychologistId = db.Psychologists
                    .Where(p => p.UserId == u.Id)
                    .Select(p => (Guid?)p.Id)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. users.Select(u => new UserResponse(
                u.Id,
                u.FullName,
                u.Email!,
                u.Roles,
                u.PsychologistId,
                u.MustChangePassword,
                u.LockoutEnd > now)),
        ];
    }
}

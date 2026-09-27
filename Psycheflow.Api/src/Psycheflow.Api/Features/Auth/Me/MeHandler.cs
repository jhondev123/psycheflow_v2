using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Auth.Me;

public sealed class MeHandler(AppDbContext db, UserManager<User> userManager, ICurrentUser currentUser)
{
    public async Task<Result<MeResponse>> Handle(CancellationToken cancellationToken)
    {
        User? user = await db.Users
            .Include(u => u.Company)
            .SingleOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        Guid? psychologistId = await db.Psychologists
            .Where(p => p.UserId == user.Id)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new MeResponse(
            user.Id,
            user.FullName,
            user.Email!,
            [.. roles],
            user.CompanyId,
            user.Company!.Name,
            psychologistId,
            user.MustChangePassword);
    }
}

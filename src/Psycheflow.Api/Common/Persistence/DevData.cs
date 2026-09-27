using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Common.Persistence;

/// <summary>
/// Dados de demonstração para desenvolvimento local (executado pelo EF via <c>UseAsyncSeeding</c> ao migrar,
/// somente no ambiente Development). É idempotente: se a empresa demo já existe, não faz nada.
/// </summary>
/// <remarks>
/// Logins de demonstração (senha de todos: <see cref="DemoPassword"/>):
/// admin@psycheflow.dev (Admin), ana@psycheflow.dev (Admin + Psicóloga), bruno@psycheflow.dev (Psicólogo),
/// gestao@psycheflow.dev (Manager).
/// </remarks>
internal static class DevData
{
    public const string DemoPassword = "Psycheflow@123";

    public static async Task SeedAsync(DbContext context, bool storeManagementPerformed, CancellationToken cancellationToken)
    {
        var db = (AppDbContext)context;

        if (await db.Users.AnyAsync(u => u.Email == "admin@psycheflow.dev", cancellationToken))
        {
            return;
        }

        Company company = Company.Create("Clínica Psycheflow (demo)").Value;
        db.Companies.Add(company);

        var hasher = new PasswordHasher<User>();
        AddUser(db, hasher, company.Id, "Administrador Demo", "admin@psycheflow.dev", Roles.Admin);
        AddUser(db, hasher, company.Id, "Ana Souza", "ana@psycheflow.dev", Roles.Admin, Roles.Psychologist);
        AddUser(db, hasher, company.Id, "Bruno Lima", "bruno@psycheflow.dev", Roles.Psychologist);
        AddUser(db, hasher, company.Id, "Gestão Demo", "gestao@psycheflow.dev", Roles.Manager);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static User AddUser(AppDbContext db, PasswordHasher<User> hasher, Guid companyId, string name, string email, params string[] roles)
    {
        var user = User.Create(name, email, companyId, mustChangePassword: false);
        user.NormalizedEmail = user.Email!.ToUpperInvariant();
        user.NormalizedUserName = user.UserName!.ToUpperInvariant();
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.EmailConfirmed = true;
        user.PasswordHash = hasher.HashPassword(user, DemoPassword);
        db.Users.Add(user);

        foreach (string role in roles)
        {
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = IdentityModel.RoleIds[role] });
        }

        return user;
    }
}

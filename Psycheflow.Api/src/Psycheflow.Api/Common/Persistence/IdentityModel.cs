using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Common.Persistence;

/// <summary>Nomes de tabela do Identity e dados essenciais (roles), que entram nas migrations via HasData.</summary>
internal static class IdentityModel
{
    /// <summary>Ids fixos: HasData precisa de valores determinísticos para não gerar migrations a cada build.</summary>
    public static readonly IReadOnlyDictionary<string, Guid> RoleIds = new Dictionary<string, Guid>
    {
        [Roles.Admin] = new("0199a1b0-0000-7000-8000-000000000001"),
        [Roles.Manager] = new("0199a1b0-0000-7000-8000-000000000002"),
        [Roles.Psychologist] = new("0199a1b0-0000-7000-8000-000000000003"),
        [Roles.Patient] = new("0199a1b0-0000-7000-8000-000000000004"),
    };

    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<User>().ToTable("users");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.Entity<IdentityRole<Guid>>().HasData(RoleIds.Select(role => new IdentityRole<Guid>
        {
            Id = role.Value,
            Name = role.Key,
            NormalizedName = role.Key.ToUpperInvariant(),
            ConcurrencyStamp = role.Value.ToString(),
        }));
    }
}

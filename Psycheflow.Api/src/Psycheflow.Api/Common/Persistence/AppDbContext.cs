using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.PsychologicalReports;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Recurrences;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Common.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    private static readonly MethodInfo ApplySoftDeleteFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo ApplyTenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Psychologist> Psychologists => Set<Psychologist>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Schedule> Schedules => Set<Schedule>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Recurrence> Recurrences => Set<Recurrence>();

    public DbSet<PsychologicalReport> PsychologicalReports => Set<PsychologicalReport>();

    /// <summary>
    /// Empresa usada pelo filtro global. Sem usuário autenticado é nula e nenhum dado de empresa é retornado.
    /// É lida a cada consulta (o EF parametriza membros do próprio DbContext).
    /// </summary>
    private Guid? CurrentCompanyId => currentUser.IsAuthenticated ? currentUser.CompanyId : null;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        IdentityModel.Configure(builder);
        ApplyGlobalFilters(builder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Textos sem tamanho explícito viram "text" no Postgres; decimais monetários com 2 casas.
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }

    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        foreach (IMutableEntityType entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            Type clrType = entityType.ClrType;
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                ApplySoftDeleteFilterMethod.MakeGenericMethod(clrType).Invoke(null, [builder]);
            }

            if (typeof(ITenantEntity).IsAssignableFrom(clrType))
            {
                ApplyTenantFilterMethod.MakeGenericMethod(clrType).Invoke(this, [builder]);
            }
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ISoftDeletable =>
        builder.Entity<TEntity>().HasQueryFilter(
            QueryFilters.SoftDelete,
            entity => EF.Property<DateTimeOffset?>(entity, nameof(ISoftDeletable.DeletedAt)) == null);

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity =>
        builder.Entity<TEntity>().HasQueryFilter(
            QueryFilters.Tenant,
            entity => EF.Property<Guid>(entity, nameof(ITenantEntity.CompanyId)) == CurrentCompanyId);
}

/// <summary>Nomes dos filtros globais, para desligá-los pontualmente com <c>IgnoreQueryFilters([...])</c>.</summary>
public static class QueryFilters
{
    public const string SoftDelete = nameof(SoftDelete);
    public const string Tenant = nameof(Tenant);
}

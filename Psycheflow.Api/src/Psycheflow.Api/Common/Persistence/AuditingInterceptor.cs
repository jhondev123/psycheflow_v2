using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Common.Persistence;

/// <summary>
/// Antes de salvar: preenche datas de auditoria (UTC), completa o <c>CompanyId</c> de novos registros com a empresa
/// do usuário logado e transforma exclusões de <see cref="ISoftDeletable"/> em exclusão lógica.
/// </summary>
public sealed class AuditingInterceptor(TimeProvider timeProvider, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        foreach (EntityEntry entry in context.ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    OnAdded(entry, now);
                    break;
                case EntityState.Modified:
                    SetIfExists(entry, nameof(IAuditable.UpdatedAt), now);
                    break;
                case EntityState.Deleted when entry.Entity is ISoftDeletable:
                    entry.State = EntityState.Modified;
                    SetIfExists(entry, nameof(ISoftDeletable.DeletedAt), now);
                    SetIfExists(entry, nameof(IAuditable.UpdatedAt), now);
                    KeepOwnedEntries(entry);
                    break;
            }
        }
    }

    private void OnAdded(EntityEntry entry, DateTimeOffset now)
    {
        if (entry.Entity is IAuditable)
        {
            entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
        }

        if (entry.Entity is ITenantEntity { CompanyId: var companyId } && companyId == Guid.Empty)
        {
            entry.Property(nameof(ITenantEntity.CompanyId)).CurrentValue = currentUser.CompanyId;
        }
    }

    /// <summary>
    /// Ao remover o dono, o EF marca os tipos owned como excluídos (o que apagaria as colunas/linhas deles).
    /// Numa exclusão lógica eles precisam continuar como estão.
    /// </summary>
    private static void KeepOwnedEntries(EntityEntry owner)
    {
        foreach (NavigationEntry navigation in owner.Navigations)
        {
            if (!navigation.Metadata.TargetEntityType.IsOwned())
            {
                continue;
            }

            IEnumerable<object> targets = navigation switch
            {
                ReferenceEntry { CurrentValue: { } value } => [value],
                CollectionEntry { CurrentValue: { } values } => values.Cast<object>(),
                _ => [],
            };

            foreach (object target in targets)
            {
                EntityEntry ownedEntry = owner.Context.Entry(target);
                if (ownedEntry.State == EntityState.Deleted)
                {
                    ownedEntry.State = EntityState.Unchanged;
                    KeepOwnedEntries(ownedEntry);
                }
            }
        }
    }

    private static void SetIfExists(EntityEntry entry, string propertyName, DateTimeOffset value)
    {
        if (entry.Metadata.FindProperty(propertyName) is not null)
        {
            entry.Property(propertyName).CurrentValue = value;
        }
    }
}

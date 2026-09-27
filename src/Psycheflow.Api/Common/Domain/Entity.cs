namespace Psycheflow.Api.Common.Domain;

/// <summary>
/// Base das entidades de domínio. O Id é gerado na criação (UUID v7, ordenável por tempo)
/// e as datas de auditoria são preenchidas pelo <c>AuditingInterceptor</c>.
/// </summary>
public abstract class Entity : IAuditable
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }
}

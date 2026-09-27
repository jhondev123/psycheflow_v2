namespace Psycheflow.Api.Common.Domain;

/// <summary>Entidades com <c>CreatedAt</c>/<c>UpdatedAt</c> preenchidos automaticamente (UTC).</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset? UpdatedAt { get; }
}

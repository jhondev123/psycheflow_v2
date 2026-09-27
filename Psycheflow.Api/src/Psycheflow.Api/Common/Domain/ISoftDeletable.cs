namespace Psycheflow.Api.Common.Domain;

/// <summary>
/// Exclusão lógica: <c>DbContext.Remove</c> vira um UPDATE de <c>DeletedAt</c> e um filtro global
/// esconde os registros excluídos de todas as consultas.
/// </summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
}

namespace Psycheflow.Api.Common.Domain;

/// <summary>
/// Dado que pertence a uma empresa. Um filtro global restringe as consultas à empresa do usuário logado
/// e o <c>CompanyId</c> é preenchido automaticamente na inserção quando não informado.
/// </summary>
public interface ITenantEntity
{
    Guid CompanyId { get; }
}

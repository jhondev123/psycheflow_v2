using System.Security.Claims;

namespace Psycheflow.Api.Common.Auth;

/// <summary>Usuário autenticado da requisição atual, lido das claims do JWT.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Id do usuário. Lança exceção se não houver usuário autenticado.</summary>
    Guid UserId { get; }

    /// <summary>Empresa do usuário. Lança exceção se não houver usuário autenticado.</summary>
    Guid CompanyId { get; }

    /// <summary>Perfil de psicólogo do usuário, quando existir.</summary>
    Guid? PsychologistId { get; }

    bool IsInRole(string role);
}

public static class CurrentUserExtensions
{
    public static bool IsManagement(this ICurrentUser user) =>
        user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager);

    /// <summary>Admin/Manager gerenciam qualquer psicólogo da empresa; o psicólogo gerencia apenas o próprio perfil e agenda.</summary>
    public static bool CanManagePsychologist(this ICurrentUser user, Guid psychologistId) =>
        user.IsManagement() || user.PsychologistId == psychologistId;
}

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId => GetRequiredGuid(AuthClaims.UserId);

    public Guid CompanyId => GetRequiredGuid(AuthClaims.CompanyId);

    public Guid? PsychologistId =>
        Guid.TryParse(Principal?.FindFirstValue(AuthClaims.PsychologistId), out Guid id) ? id : null;

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    private Guid GetRequiredGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out Guid value)
            ? value
            : throw new InvalidOperationException($"Claim '{claimType}' ausente: a requisição não está autenticada.");
}

using Microsoft.AspNetCore.Authorization;

namespace Psycheflow.Api.Common.Auth;

/// <summary>
/// Políticas de autorização. Todas (exceto <see cref="PendingPasswordChange"/>) exigem que o usuário
/// já tenha trocado a senha temporária.
/// </summary>
public static class Policies
{
    /// <summary>Qualquer usuário interno da empresa (Admin, Manager ou Psychologist). É a política padrão.</summary>
    public const string Staff = nameof(Staff);

    /// <summary>Gestão da empresa (Admin ou Manager).</summary>
    public const string Management = nameof(Management);

    /// <summary>Apenas autenticado — usada pela troca obrigatória de senha.</summary>
    public const string PendingPasswordChange = nameof(PendingPasswordChange);

    public static IServiceCollection AddPsycheflowAuthorization(this IServiceCollection services)
    {
        AuthorizationPolicy staff = BuildPolicy(Roles.Admin, Roles.Manager, Roles.Psychologist);

        // A política de fallback torna todo endpoint protegido por padrão; públicos usam AllowAnonymous().
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(staff)
            .SetFallbackPolicy(staff)
            .AddPolicy(Staff, staff)
            .AddPolicy(Management, BuildPolicy(Roles.Admin, Roles.Manager))
            .AddPolicy(PendingPasswordChange, policy => policy.RequireAuthenticatedUser());

        return services;
    }

    private static AuthorizationPolicy BuildPolicy(params string[] roles) =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(roles)
            .RequireAssertion(context => !context.User.HasClaim(AuthClaims.MustChangePassword, "true"))
            .Build();
}

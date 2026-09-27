using Microsoft.AspNetCore.Identity;
using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "auth.invalid_credentials", "E-mail ou senha inválidos.");

    public static readonly Error LockedOut = Error.Locked(
        "auth.locked_out", "Conta bloqueada temporariamente por excesso de tentativas. Tente novamente em alguns minutos.");

    public static readonly Error EmailInUse = Error.Conflict(
        "auth.email_in_use", "Este e-mail já está em uso.");

    public static readonly Error UserNotFound = Error.NotFound(
        "auth.user_not_found", "Usuário não encontrado.");

    /// <summary>Converte a falha do ASP.NET Identity no erro de negócio correspondente (campo em camelCase).</summary>
    public static Error FromIdentity(IdentityResult result, string passwordField = "password")
    {
        IdentityError[] errors = [.. result.Errors];
        IdentityError first = errors.FirstOrDefault() ?? new IdentityError { Code = "Unknown", Description = "Falha ao salvar o usuário." };

        if (first.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName))
        {
            return EmailInUse;
        }

        if (first.Code == nameof(IdentityErrorDescriber.PasswordMismatch))
        {
            return Error.Validation("auth.password_mismatch", "A senha atual está incorreta.", "currentPassword");
        }

        if (first.Code.StartsWith("Password", StringComparison.Ordinal))
        {
            string message = string.Join(' ', errors.Where(e => e.Code.StartsWith("Password", StringComparison.Ordinal)).Select(e => e.Description));
            return Error.Validation("auth.weak_password", message, passwordField);
        }

        if (first.Code is nameof(IdentityErrorDescriber.InvalidEmail) or nameof(IdentityErrorDescriber.InvalidUserName))
        {
            return Error.Validation("auth.invalid_email", first.Description, "email");
        }

        return Error.Validation($"auth.{first.Code}", first.Description);
    }
}

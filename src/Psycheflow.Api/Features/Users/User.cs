using Microsoft.AspNetCore.Identity;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Companies;

namespace Psycheflow.Api.Features.Users;

/// <summary>
/// Usuário que acessa o sistema (ASP.NET Identity). O e-mail é o login.
/// Não usa o filtro automático de empresa porque o login precisa localizá-lo antes de existir um usuário autenticado;
/// consultas de usuários filtram <see cref="CompanyId"/> explicitamente.
/// </summary>
public sealed class User : IdentityUser<Guid>, IAuditable
{
    public const int FullNameMaxLength = 150;

    private User()
    {
    }

    public string FullName { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    public Company? Company { get; private set; }

    /// <summary>Usuário criado por Admin/Manager com senha temporária precisa trocá-la no primeiro acesso.</summary>
    public bool MustChangePassword { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static User Create(string fullName, string email, Guid companyId, bool mustChangePassword)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();
        return new User
        {
            Id = Guid.CreateVersion7(),
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            CompanyId = companyId,
            MustChangePassword = mustChangePassword,
            LockoutEnabled = true,
        };
    }

    public void Rename(string fullName) => FullName = fullName.Trim();

    public void PasswordChanged() => MustChangePassword = false;
}

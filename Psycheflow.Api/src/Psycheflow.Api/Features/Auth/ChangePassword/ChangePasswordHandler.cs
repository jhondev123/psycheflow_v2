using Microsoft.AspNetCore.Identity;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Auth.ChangePassword;

/// <summary>Troca a senha (inclusive a temporária do primeiro acesso) e devolve um token novo, já sem a restrição.</summary>
public sealed class ChangePasswordHandler(UserManager<User> userManager, ICurrentUser currentUser, AccessTokenIssuer tokenIssuer)
{
    public async Task<Result<AuthResponse>> Handle(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        // Marcado antes: o ChangePasswordAsync persiste o usuário junto com a nova senha (e nada é salvo se falhar).
        user.PasswordChanged();
        IdentityResult changed = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changed.Succeeded)
        {
            return AuthErrors.FromIdentity(changed, passwordField: "newPassword");
        }

        return await tokenIssuer.IssueAsync(user, cancellationToken);
    }
}

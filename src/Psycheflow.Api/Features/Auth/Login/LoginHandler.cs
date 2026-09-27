using Microsoft.AspNetCore.Identity;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Auth.Login;

/// <summary>UC23 / RN-10 a RN-12: login por e-mail e senha com bloqueio temporário após tentativas erradas.</summary>
public sealed class LoginHandler(UserManager<User> userManager, AccessTokenIssuer tokenIssuer)
{
    public async Task<Result<AuthResponse>> Handle(LoginRequest request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthErrors.LockedOut;
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return await userManager.IsLockedOutAsync(user) ? AuthErrors.LockedOut : AuthErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await tokenIssuer.IssueAsync(user, cancellationToken);
    }
}

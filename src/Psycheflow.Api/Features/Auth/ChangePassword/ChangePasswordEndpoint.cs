using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Auth.ChangePassword;

public static class ChangePasswordEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/change-password", HandleAsync)
            .RequireAuthorization(Policies.PendingPasswordChange)
            .WithName("ChangePassword")
            .WithSummary("Troca a senha do usuário logado (obrigatório no primeiro acesso com senha temporária).")
            .WithRequestValidation<ChangePasswordRequest>();

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> HandleAsync(
        ChangePasswordRequest request, ChangePasswordHandler handler, CancellationToken cancellationToken)
    {
        Result<AuthResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

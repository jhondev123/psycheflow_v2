using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Auth.Me;

public static class MeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/me", HandleAsync)
            .RequireAuthorization(Policies.PendingPasswordChange)
            .WithName("GetMe")
            .WithSummary("Dados do usuário logado: perfis, empresa e id do perfil de psicólogo.");

    private static async Task<Results<Ok<MeResponse>, ProblemHttpResult>> HandleAsync(
        MeHandler handler, CancellationToken cancellationToken)
    {
        Result<MeResponse> result = await handler.Handle(cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Auth.Login;

public static class LoginEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/login", HandleAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Autentica com e-mail e senha e devolve o token de acesso.")
            .WithRequestValidation<LoginRequest>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status423Locked);

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> HandleAsync(
        LoginRequest request, LoginHandler handler, CancellationToken cancellationToken)
    {
        Result<AuthResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

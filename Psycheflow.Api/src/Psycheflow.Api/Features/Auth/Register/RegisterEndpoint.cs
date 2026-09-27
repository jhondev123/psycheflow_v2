using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/register", HandleAsync)
            .AllowAnonymous()
            .WithName("Register")
            .WithSummary("Cria a conta: empresa, usuário responsável (Admin + Psicólogo) e perfil profissional.")
            .WithRequestValidation<RegisterRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<AuthResponse>, ProblemHttpResult>> HandleAsync(
        RegisterRequest request, RegisterHandler handler, CancellationToken cancellationToken)
    {
        Result<AuthResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created("/api/v1/auth/me", result.Value)
            : result.Error.ToProblem();
    }
}

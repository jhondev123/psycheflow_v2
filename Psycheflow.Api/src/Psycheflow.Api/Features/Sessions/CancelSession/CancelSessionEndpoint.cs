using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Sessions.CancelSession;

public static class CancelSessionEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/{id:guid}/cancel", HandleAsync)
            .WithName("CancelSession")
            .WithSummary("Cancela a sessão com motivo obrigatório; o horário fica livre.")
            .WithRequestValidation<CancelSessionRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, CancelSessionRequest request, CancelSessionHandler handler, CancellationToken cancellationToken)
    {
        Result<SessionResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

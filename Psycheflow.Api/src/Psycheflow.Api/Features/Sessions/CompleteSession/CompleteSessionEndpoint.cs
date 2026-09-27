using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Sessions.CompleteSession;

public static class CompleteSessionEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/{id:guid}/complete", HandleAsync)
            .WithName("CompleteSession")
            .WithSummary("Conclui a sessão com anotações (Markdown) e feedback de 0 a 10 (psicólogo da sessão).")
            .WithRequestValidation<CompleteSessionRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, CompleteSessionRequest request, CompleteSessionHandler handler, CancellationToken cancellationToken)
    {
        Result<SessionResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

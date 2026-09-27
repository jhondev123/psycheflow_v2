using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Sessions.ConfirmSession;

public static class ConfirmSessionEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/{id:guid}/confirm", HandleAsync)
            .WithName("ConfirmSession")
            .WithSummary("Confirma o horário de uma sessão agendada.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, ConfirmSessionHandler handler, CancellationToken cancellationToken)
    {
        Result<SessionResponse> result = await handler.Handle(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

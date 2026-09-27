using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Sessions.UpdateSession;

public static class UpdateSessionEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateSession")
            .WithSummary("Altera paciente e anotações de uma sessão agendada.")
            .WithRequestValidation<UpdateSessionRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, UpdateSessionRequest request, UpdateSessionHandler handler, CancellationToken cancellationToken)
    {
        Result<SessionResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

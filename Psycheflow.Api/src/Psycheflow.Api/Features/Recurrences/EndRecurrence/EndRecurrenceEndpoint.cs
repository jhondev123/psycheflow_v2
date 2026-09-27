using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Recurrences.EndRecurrence;

public static class EndRecurrenceEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/{id:guid}/end", HandleAsync)
            .WithName("EndRecurrence")
            .WithSummary("Encerra a recorrência e cancela as sessões futuras ainda agendadas.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<RecurrenceResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, EndRecurrenceRequest request, EndRecurrenceHandler handler, CancellationToken cancellationToken)
    {
        Result<RecurrenceResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

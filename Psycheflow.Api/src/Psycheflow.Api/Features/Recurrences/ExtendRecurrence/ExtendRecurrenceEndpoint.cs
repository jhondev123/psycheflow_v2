using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Recurrences.ExtendRecurrence;

public static class ExtendRecurrenceEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/{id:guid}/extend", HandleAsync)
            .WithName("ExtendRecurrence")
            .WithSummary("Gera as sessões dos próximos 3 meses de uma recorrência ativa.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<RecurrenceGenerationResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, ExtendRecurrenceHandler handler, CancellationToken cancellationToken)
    {
        Result<RecurrenceGenerationResponse> result = await handler.Handle(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

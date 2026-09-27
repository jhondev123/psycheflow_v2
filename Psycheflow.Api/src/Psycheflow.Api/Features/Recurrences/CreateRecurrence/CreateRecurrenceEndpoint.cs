using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Recurrences.CreateRecurrence;

public static class CreateRecurrenceEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", HandleAsync)
            .WithName("CreateRecurrence")
            .WithSummary("Cria recorrência semanal ou mensal e agenda as sessões dos próximos 3 meses (datas indisponíveis são puladas).")
            .WithRequestValidation<CreateRecurrenceRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Created<RecurrenceGenerationResponse>, ProblemHttpResult>> HandleAsync(
        CreateRecurrenceRequest request, CreateRecurrenceHandler handler, CancellationToken cancellationToken)
    {
        Result<RecurrenceGenerationResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/recurrences/{result.Value.Recurrence.Id}", result.Value)
            : result.Error.ToProblem();
    }
}

using Microsoft.AspNetCore.Http.HttpResults;

namespace Psycheflow.Api.Features.Recurrences.ListRecurrences;

public static class ListRecurrencesEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
            .WithName("ListRecurrences")
            .WithSummary("Recorrências (por paciente, opcionalmente só as ativas).");

    private static async Task<Ok<IReadOnlyList<RecurrenceResponse>>> HandleAsync(
        [AsParameters] ListRecurrencesQuery query, ListRecurrencesHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(query, cancellationToken));
}

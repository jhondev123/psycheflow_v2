using Microsoft.AspNetCore.Http.HttpResults;

namespace Psycheflow.Api.Features.Psychologists.ListPsychologists;

public static class ListPsychologistsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
            .WithName("ListPsychologists")
            .WithSummary("Lista os psicólogos da empresa com o expediente de cada um.");

    private static async Task<Ok<IReadOnlyList<PsychologistResponse>>> HandleAsync(
        ListPsychologistsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));
}

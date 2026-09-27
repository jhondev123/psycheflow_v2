using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Scheduling.GetAgenda;

public static class GetAgendaEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
            .WithName("GetAgenda")
            .WithSummary("Agenda do período: sessões, bloqueios e expediente (até 62 dias).")
            .WithRequestValidation<AgendaQuery>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

    private static async Task<Results<Ok<AgendaResponse>, ProblemHttpResult>> HandleAsync(
        [AsParameters] AgendaQuery query, GetAgendaHandler handler, CancellationToken cancellationToken)
    {
        Result<AgendaResponse> result = await handler.Handle(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

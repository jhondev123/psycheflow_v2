using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Sessions.ListSessions;

public static class ListSessionsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
            .WithName("ListSessions")
            .WithSummary("Lista paginada de sessões por período, horário, paciente, psicólogo e status.")
            .WithRequestValidation<ListSessionsQuery>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

    private static async Task<Results<Ok<PagedResponse<SessionListItem>>, ProblemHttpResult>> HandleAsync(
        [AsParameters] ListSessionsQuery query, ListSessionsHandler handler, CancellationToken cancellationToken)
    {
        Result<PagedResponse<SessionListItem>> result = await handler.Handle(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Scheduling.DeleteBlock;

public static class DeleteBlockEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteScheduleBlock")
            .WithSummary("Remove um bloqueio de agenda, liberando o horário.")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id, DeleteBlockHandler handler, CancellationToken cancellationToken)
    {
        Result result = await handler.Handle(id, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}

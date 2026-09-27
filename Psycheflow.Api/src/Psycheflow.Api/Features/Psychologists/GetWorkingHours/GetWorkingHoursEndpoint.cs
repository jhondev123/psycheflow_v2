using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Psychologists.GetWorkingHours;

public static class GetWorkingHoursEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/{id:guid}/working-hours", HandleAsync)
            .WithName("GetWorkingHours")
            .WithSummary("Expediente semanal do psicólogo, ordenado por dia e horário.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<IReadOnlyList<WorkingHoursDto>>, ProblemHttpResult>> HandleAsync(
        Guid id, GetWorkingHoursHandler handler, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<WorkingHoursDto>> result = await handler.Handle(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

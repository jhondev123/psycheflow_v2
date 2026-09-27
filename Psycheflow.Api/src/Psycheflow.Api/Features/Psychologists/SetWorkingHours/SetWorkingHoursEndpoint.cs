using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Psychologists.SetWorkingHours;

public static class SetWorkingHoursEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPut("/{id:guid}/working-hours", HandleAsync)
            .WithName("SetWorkingHours")
            .WithSummary("Substitui o expediente semanal do psicólogo (o próprio ou Admin/Manager).")
            .WithRequestValidation<SetWorkingHoursRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<IReadOnlyList<WorkingHoursDto>>, ProblemHttpResult>> HandleAsync(
        Guid id, SetWorkingHoursRequest request, SetWorkingHoursHandler handler, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<WorkingHoursDto>> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

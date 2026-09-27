using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.PsychologicalReports.UpdateReport;

public static class UpdateReportEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdatePsychologicalReport")
            .WithSummary("Edita um laudo/relatório em rascunho.")
            .WithRequestValidation<UpdateReportRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<PsychologicalReportResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, UpdateReportRequest request, UpdateReportHandler handler, CancellationToken cancellationToken)
    {
        Result<PsychologicalReportResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.PsychologicalReports.CreateReport;

public static class CreateReportEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", HandleAsync)
            .WithName("CreatePsychologicalReport")
            .WithSummary("Cria o rascunho de um laudo ou relatório psicológico (CFP 06/2019).")
            .WithRequestValidation<CreateReportRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Created<PsychologicalReportResponse>, ProblemHttpResult>> HandleAsync(
        CreateReportRequest request, CreateReportHandler handler, CancellationToken cancellationToken)
    {
        Result<PsychologicalReportResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/psychological-reports/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }
}

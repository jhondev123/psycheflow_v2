using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Documents.FeedbackReport;

public static class FeedbackReportEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/feedback-report", HandleAsync)
            .WithName("GetFeedbackReportPdf")
            .WithSummary("Relatório em PDF dos feedbacks (0–10) do paciente nas sessões com o psicólogo logado.")
            .WithRequestValidation<FeedbackReportQuery>()
            .ProducesPdf()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> HandleAsync(
        [AsParameters] FeedbackReportQuery query, FeedbackReportHandler handler, CancellationToken cancellationToken) =>
        (await handler.Handle(query, cancellationToken)).ToFileResult();
}

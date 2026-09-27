using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Documents.SessionsReport;

public static class SessionsReportEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/sessions-report", HandleAsync)
            .WithName("GetSessionsReportPdf")
            .WithSummary("Relatório de sessões em PDF (período, status da sessão e do pagamento).")
            .WithRequestValidation<SessionsReportQuery>()
            .ProducesPdf()
            .ProducesProblem(StatusCodes.Status403Forbidden);

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> HandleAsync(
        [AsParameters] SessionsReportQuery query, SessionsReportHandler handler, CancellationToken cancellationToken) =>
        (await handler.Handle(query, cancellationToken)).ToFileResult();
}

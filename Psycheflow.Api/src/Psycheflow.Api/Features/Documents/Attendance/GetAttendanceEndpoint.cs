using Microsoft.AspNetCore.Http.HttpResults;

namespace Psycheflow.Api.Features.Documents.Attendance;

public static class GetAttendanceEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/attendance/{sessionId:guid}", HandleAsync)
            .WithName("GetAttendanceDeclarationPdf")
            .WithSummary("Declaração de comparecimento em PDF de uma sessão concluída.")
            .ProducesPdf()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> HandleAsync(
        Guid sessionId, GetAttendanceHandler handler, CancellationToken cancellationToken) =>
        (await handler.Handle(sessionId, cancellationToken)).ToFileResult();
}

using Microsoft.AspNetCore.Http.HttpResults;

namespace Psycheflow.Api.Features.Documents.Receipt;

public static class GetReceiptEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/receipts/{paymentId:guid}", HandleAsync)
            .WithName("GetReceiptPdf")
            .WithSummary("Recibo em PDF de um pagamento recebido.")
            .ProducesPdf()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> HandleAsync(
        Guid paymentId, GetReceiptHandler handler, CancellationToken cancellationToken) =>
        (await handler.Handle(paymentId, cancellationToken)).ToFileResult();
}

using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Payments.ListPayments;

public static class ListPaymentsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
            .WithName("ListPayments")
            .WithSummary("Lista paginada de pagamentos por período da sessão, paciente, psicólogo e status.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

    private static async Task<Results<Ok<PagedResponse<PaymentResponse>>, ProblemHttpResult>> HandleAsync(
        [AsParameters] ListPaymentsQuery query, ListPaymentsHandler handler, CancellationToken cancellationToken)
    {
        Result<PagedResponse<PaymentResponse>> result = await handler.Handle(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

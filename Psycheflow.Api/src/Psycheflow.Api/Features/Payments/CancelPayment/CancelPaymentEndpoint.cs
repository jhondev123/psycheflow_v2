using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Payments.CancelPayment;

public static class CancelPaymentEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/{id:guid}/cancel", HandleAsync)
            .WithName("CancelPayment")
            .WithSummary("Cancela um pagamento pendente ou estorna um pago (motivo obrigatório).")
            .WithRequestValidation<CancelPaymentRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<PaymentResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, CancelPaymentRequest request, CancelPaymentHandler handler, CancellationToken cancellationToken)
    {
        Result<PaymentResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Payments.UpdatePayment;

public static class UpdatePaymentEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdatePayment")
            .WithSummary("Altera o valor de um pagamento pendente (pago não pode ser editado).")
            .WithRequestValidation<UpdatePaymentRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<PaymentResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, UpdatePaymentRequest request, UpdatePaymentHandler handler, CancellationToken cancellationToken)
    {
        Result<PaymentResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

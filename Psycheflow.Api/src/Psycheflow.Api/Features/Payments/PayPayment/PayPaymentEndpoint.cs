using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Payments.PayPayment;

public static class PayPaymentEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/{id:guid}/pay", HandleAsync)
            .WithName("PayPayment")
            .WithSummary("Lança o pagamento de uma sessão concluída (método, data e valor).")
            .WithRequestValidation<PayPaymentRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<PaymentResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, PayPaymentRequest request, PayPaymentHandler handler, CancellationToken cancellationToken)
    {
        Result<PaymentResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

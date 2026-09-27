using Psycheflow.Api.Features.Payments.CancelPayment;
using Psycheflow.Api.Features.Payments.GetPayment;
using Psycheflow.Api.Features.Payments.ListPayments;
using Psycheflow.Api.Features.Payments.PayPayment;
using Psycheflow.Api.Features.Payments.UpdatePayment;

namespace Psycheflow.Api.Features.Payments;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/payments").WithTags("Payments");

        ListPaymentsEndpoint.Map(group);
        GetPaymentEndpoint.Map(group);
        UpdatePaymentEndpoint.Map(group);
        PayPaymentEndpoint.Map(group);
        CancelPaymentEndpoint.Map(group);

        return api;
    }
}

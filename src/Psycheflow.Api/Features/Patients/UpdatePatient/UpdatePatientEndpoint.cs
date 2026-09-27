using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Patients.UpdatePatient;

public static class UpdatePatientEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdatePatient")
            .WithSummary("Atualiza dados, endereço e status do paciente (o CPF não é editável).")
            .WithRequestValidation<UpdatePatientRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<PatientResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, UpdatePatientRequest request, UpdatePatientHandler handler, CancellationToken cancellationToken)
    {
        Result<PatientResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

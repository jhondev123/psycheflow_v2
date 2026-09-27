using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Patients.GetPatient;

public static class GetPatientEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetPatient")
            .WithSummary("Dados completos de um paciente da empresa.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<PatientResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, GetPatientHandler handler, CancellationToken cancellationToken)
    {
        Result<PatientResponse> result = await handler.Handle(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

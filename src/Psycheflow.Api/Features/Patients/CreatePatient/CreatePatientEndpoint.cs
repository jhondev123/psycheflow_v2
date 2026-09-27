using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Patients.CreatePatient;

public static class CreatePatientEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", HandleAsync)
            .WithName("CreatePatient")
            .WithSummary("Cadastra um paciente (nome, CPF, e-mail e telefone obrigatórios).")
            .WithRequestValidation<CreatePatientRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<PatientResponse>, ProblemHttpResult>> HandleAsync(
        CreatePatientRequest request, CreatePatientHandler handler, CancellationToken cancellationToken)
    {
        Result<PatientResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/patients/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }
}

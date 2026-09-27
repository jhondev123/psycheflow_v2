using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Patients.ListPatients;

public static class ListPatientsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
            .WithName("ListPatients")
            .WithSummary("Lista paginada de pacientes com busca (nome, e-mail ou CPF) e filtros.");

    private static async Task<Ok<PagedResponse<PatientListItem>>> HandleAsync(
        [AsParameters] ListPatientsQuery query, ListPatientsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(query, cancellationToken));
}

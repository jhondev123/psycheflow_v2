using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Psychologists.GetPsychologist;

public static class GetPsychologistEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetPsychologist")
            .WithSummary("Perfil de um psicólogo da empresa.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<PsychologistResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, GetPsychologistHandler handler, CancellationToken cancellationToken)
    {
        Result<PsychologistResponse> result = await handler.Handle(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

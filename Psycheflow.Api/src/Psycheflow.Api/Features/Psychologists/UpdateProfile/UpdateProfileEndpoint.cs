using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Psychologists.UpdateProfile;

public static class UpdateProfileEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdatePsychologistProfile")
            .WithSummary("Atualiza nome, CRP, abordagem e telefone do psicólogo (o próprio ou Admin/Manager).")
            .WithRequestValidation<UpdateProfileRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<PsychologistResponse>, ProblemHttpResult>> HandleAsync(
        Guid id, UpdateProfileRequest request, UpdateProfileHandler handler, CancellationToken cancellationToken)
    {
        Result<PsychologistResponse> result = await handler.Handle(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

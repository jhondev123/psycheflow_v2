using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Psychologists.GetMyProfile;

public static class GetMyProfileEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/me", HandleAsync)
            .WithName("GetMyPsychologistProfile")
            .WithSummary("Perfil de psicólogo do usuário logado.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<PsychologistResponse>, ProblemHttpResult>> HandleAsync(
        GetMyProfileHandler handler, CancellationToken cancellationToken)
    {
        Result<PsychologistResponse> result = await handler.Handle(cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

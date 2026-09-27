using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Ai.UpdateAiSettings;

public static class UpdateAiSettingsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPut("/settings", HandleAsync)
            .RequireAuthorization(Policies.Management)
            .WithName("UpdateAiSettings")
            .WithSummary("Habilita/desabilita a IA, escolhe o provedor e os dados que podem ser enviados (Admin/Manager).")
            .WithRequestValidation<UpdateAiSettingsRequest>();

    private static async Task<Results<Ok<AiSettingsResponse>, ProblemHttpResult>> HandleAsync(
        UpdateAiSettingsRequest request, UpdateAiSettingsHandler handler, CancellationToken cancellationToken)
    {
        Result<AiSettingsResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Companies.UpdateSettings;

public static class UpdateSettingsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPut("/", HandleAsync)
            .RequireAuthorization(Policies.Management)
            .WithName("UpdateSettings")
            .WithSummary("Altera as configurações da empresa (Admin/Manager).")
            .WithRequestValidation<UpdateSettingsRequest>();

    private static async Task<Results<Ok<SettingsResponse>, ProblemHttpResult>> HandleAsync(
        UpdateSettingsRequest request, UpdateSettingsHandler handler, CancellationToken cancellationToken)
    {
        Result<SettingsResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

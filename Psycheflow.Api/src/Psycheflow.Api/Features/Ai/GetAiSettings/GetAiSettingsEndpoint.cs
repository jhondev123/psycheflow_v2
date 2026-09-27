using Microsoft.AspNetCore.Http.HttpResults;

namespace Psycheflow.Api.Features.Ai.GetAiSettings;

public static class GetAiSettingsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/settings", HandleAsync)
            .WithName("GetAiSettings")
            .WithSummary("Uso da IA na clínica: se está habilitada, provedor, dados autorizados e provedores configurados no servidor.");

    private static async Task<Ok<AiSettingsResponse>> HandleAsync(GetAiSettingsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));
}

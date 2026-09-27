using Microsoft.AspNetCore.Http.HttpResults;

namespace Psycheflow.Api.Features.Companies.GetSettings;

public static class GetSettingsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
            .WithName("GetSettings")
            .WithSummary("Configurações da empresa (duração e valor padrão da sessão, fuso horário).");

    private static async Task<Ok<SettingsResponse>> HandleAsync(GetSettingsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));
}

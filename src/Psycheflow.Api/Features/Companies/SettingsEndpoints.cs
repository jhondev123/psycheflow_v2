using Psycheflow.Api.Features.Companies.GetSettings;
using Psycheflow.Api.Features.Companies.UpdateSettings;

namespace Psycheflow.Api.Features.Companies;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/settings").WithTags("Settings");

        GetSettingsEndpoint.Map(group);
        UpdateSettingsEndpoint.Map(group);

        return api;
    }
}

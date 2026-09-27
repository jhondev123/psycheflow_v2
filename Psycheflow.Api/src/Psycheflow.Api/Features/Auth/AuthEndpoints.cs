using Psycheflow.Api.Features.Auth.ChangePassword;
using Psycheflow.Api.Features.Auth.Login;
using Psycheflow.Api.Features.Auth.Me;
using Psycheflow.Api.Features.Auth.Register;

namespace Psycheflow.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/auth").WithTags("Auth");

        RegisterEndpoint.Map(group);
        LoginEndpoint.Map(group);
        ChangePasswordEndpoint.Map(group);
        MeEndpoint.Map(group);

        return api;
    }
}

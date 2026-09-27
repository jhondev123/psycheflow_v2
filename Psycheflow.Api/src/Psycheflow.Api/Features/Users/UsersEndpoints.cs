using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Users.CreateUser;
using Psycheflow.Api.Features.Users.ListUsers;

namespace Psycheflow.Api.Features.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization(Policies.Management);

        CreateUserEndpoint.Map(group);
        ListUsersEndpoint.Map(group);

        return api;
    }
}

using Microsoft.AspNetCore.Http.HttpResults;

namespace Psycheflow.Api.Features.Users.ListUsers;

public static class ListUsersEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
            .WithName("ListUsers")
            .WithSummary("Lista os usuários da empresa (Admin/Manager).");

    private static async Task<Ok<IReadOnlyList<UserResponse>>> HandleAsync(
        ListUsersHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));
}

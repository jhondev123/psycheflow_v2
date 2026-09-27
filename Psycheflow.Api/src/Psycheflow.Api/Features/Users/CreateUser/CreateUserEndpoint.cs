using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Users.CreateUser;

public static class CreateUserEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", HandleAsync)
            .WithName("CreateUser")
            .WithSummary("Cadastra um usuário da empresa com senha temporária (Admin/Manager).")
            .WithRequestValidation<CreateUserRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<CreateUserResponse>, ProblemHttpResult>> HandleAsync(
        CreateUserRequest request, CreateUserHandler handler, CancellationToken cancellationToken)
    {
        Result<CreateUserResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/users/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }
}

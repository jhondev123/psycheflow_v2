using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Sessions.CreateSession;

public static class CreateSessionEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", HandleAsync)
            .WithName("CreateSession")
            .WithSummary("Agenda uma sessão dentro do expediente do psicólogo, sem conflito de horário.")
            .WithRequestValidation<CreateSessionRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<SessionResponse>, ProblemHttpResult>> HandleAsync(
        CreateSessionRequest request, CreateSessionHandler handler, CancellationToken cancellationToken)
    {
        Result<SessionResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/sessions/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }
}

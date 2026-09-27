using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Scheduling.CreateBlock;

public static class CreateBlockEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", HandleAsync)
            .WithName("CreateScheduleBlock")
            .WithSummary("Bloqueia dias inteiros ou uma faixa de horário na agenda (um bloqueio por dia).")
            .WithRequestValidation<CreateBlockRequest>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<IReadOnlyList<BlockResponse>>, ProblemHttpResult>> HandleAsync(
        CreateBlockRequest request, CreateBlockHandler handler, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<BlockResponse>> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess ? TypedResults.Created((string?)null, result.Value) : result.Error.ToProblem();
    }
}

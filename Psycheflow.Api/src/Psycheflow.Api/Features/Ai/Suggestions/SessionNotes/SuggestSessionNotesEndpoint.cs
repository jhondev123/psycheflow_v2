using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Ai.Suggestions.SessionNotes;

public static class SuggestSessionNotesEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/suggestions/session-notes", HandleAsync)
            .WithName("SuggestSessionNotes")
            .WithSummary("Sugestão de registro de evolução a partir do rascunho das anotações da sessão (psicólogo da sessão).")
            .WithRequestValidation<SuggestSessionNotesRequest>();

    private static async Task<Results<Ok<AiSuggestionResponse>, ProblemHttpResult>> HandleAsync(
        SuggestSessionNotesRequest request, SuggestSessionNotesHandler handler, CancellationToken cancellationToken)
    {
        Result<AiSuggestionResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

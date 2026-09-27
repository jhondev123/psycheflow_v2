using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Ai.Suggestions.PatientSuggestions;

public static class SuggestForPatientEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost("/suggestions/patient-analysis", AnalyzeAsync)
            .WithName("SuggestPatientAnalysis")
            .WithSummary("Análise do acompanhamento do paciente com base nas sessões e no prontuário do psicólogo logado.")
            .WithRequestValidation<PatientSuggestionRequest>();

        group.MapPost("/suggestions/next-steps", NextStepsAsync)
            .WithName("SuggestNextSteps")
            .WithSummary("Sugestões de próximos passos para o acompanhamento do paciente.")
            .WithRequestValidation<PatientSuggestionRequest>();
    }

    private static Task<Results<Ok<AiSuggestionResponse>, ProblemHttpResult>> AnalyzeAsync(
        PatientSuggestionRequest request, SuggestForPatientHandler handler, CancellationToken cancellationToken) =>
        HandleAsync(AiSuggestionKind.PatientAnalysis, request, handler, cancellationToken);

    private static Task<Results<Ok<AiSuggestionResponse>, ProblemHttpResult>> NextStepsAsync(
        PatientSuggestionRequest request, SuggestForPatientHandler handler, CancellationToken cancellationToken) =>
        HandleAsync(AiSuggestionKind.NextSteps, request, handler, cancellationToken);

    private static async Task<Results<Ok<AiSuggestionResponse>, ProblemHttpResult>> HandleAsync(
        AiSuggestionKind kind, PatientSuggestionRequest request, SuggestForPatientHandler handler, CancellationToken cancellationToken)
    {
        Result<AiSuggestionResponse> result = await handler.Handle(kind, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

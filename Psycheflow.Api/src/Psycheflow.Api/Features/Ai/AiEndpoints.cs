using Psycheflow.Api.Common.RateLimiting;
using Psycheflow.Api.Features.Ai.GetAiSettings;
using Psycheflow.Api.Features.Ai.Suggestions.PatientSuggestions;
using Psycheflow.Api.Features.Ai.Suggestions.SessionNotes;
using Psycheflow.Api.Features.Ai.UpdateAiSettings;

namespace Psycheflow.Api.Features.Ai;

/// <summary>Assistente de IA (RF021): configuração por clínica e sugestões para o psicólogo.</summary>
public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/ai").WithTags("AI");

        GetAiSettingsEndpoint.Map(group);
        UpdateAiSettingsEndpoint.Map(group);
        // Cada sugestão consome créditos do provedor: limite por usuário (DT-23).
        RouteGroupBuilder suggestions = group.MapGroup("/suggestions").RequireRateLimiting(RateLimitPolicies.Ai);
        SuggestSessionNotesEndpoint.Map(suggestions);
        SuggestForPatientEndpoint.Map(suggestions);

        return api;
    }
}

namespace Psycheflow.Api.Features.Ai;

public sealed record AiSettingsResponse(
    bool IsEnabled,
    AiProvider Provider,
    bool ShareSessionNotes,
    bool ShareFeedbacks,
    bool ShareMedicalRecords,
    DateTimeOffset? ConsentAcceptedAt,
    IReadOnlyList<AiProvider> AvailableProviders)
{
    public static AiSettingsResponse From(AiSettings settings, IReadOnlyList<AiProvider> availableProviders) => new(
        settings.IsEnabled,
        settings.Provider,
        settings.ShareSessionNotes,
        settings.ShareFeedbacks,
        settings.ShareMedicalRecords,
        settings.ConsentAcceptedAt,
        availableProviders);
}

/// <summary>Sugestão em Markdown, sempre acompanhada do aviso de revisão pelo profissional.</summary>
public sealed record AiSuggestionResponse(
    string Suggestion,
    AiProvider Provider,
    string Model,
    DateTimeOffset GeneratedAt,
    string Disclaimer);

namespace Psycheflow.Api.Features.Ai.UpdateAiSettings;

/// <summary>Para habilitar é obrigatório aceitar os termos (<paramref name="AcceptTerms"/>): o aceite fica registrado.</summary>
public sealed record UpdateAiSettingsRequest(
    bool IsEnabled,
    AiProvider Provider,
    bool ShareSessionNotes,
    bool ShareFeedbacks,
    bool ShareMedicalRecords,
    bool AcceptTerms);

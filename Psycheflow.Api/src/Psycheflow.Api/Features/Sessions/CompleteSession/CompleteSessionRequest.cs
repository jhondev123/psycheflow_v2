namespace Psycheflow.Api.Features.Sessions.CompleteSession;

/// <param name="Notes">Anotações da sessão em Markdown (obrigatório).</param>
/// <param name="FeedbackScore">Nota de 0 a 10 (obrigatório, RN-45).</param>
public sealed record CompleteSessionRequest(string? Notes, int? FeedbackScore, string? FeedbackComment = null);

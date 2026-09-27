namespace Psycheflow.Api.Features.Ai.Suggestions.SessionNotes;

/// <summary>Sem <paramref name="Draft"/>, usa as anotações já salvas na sessão.</summary>
public sealed record SuggestSessionNotesRequest(Guid SessionId, string? Draft);

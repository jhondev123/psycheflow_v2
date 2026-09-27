namespace Psycheflow.Api.Features.Sessions.CancelSession;

/// <param name="Reason">Obrigatório (RN-43).</param>
public sealed record CancelSessionRequest(string? Reason);

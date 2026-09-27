namespace Psycheflow.Api.Features.Sessions.UpdateSession;

/// <param name="Notes">Anotações em Markdown. Nulo mantém as atuais; só o psicólogo da sessão pode alterar.</param>
public sealed record UpdateSessionRequest(Guid PatientId, string? Notes = null);

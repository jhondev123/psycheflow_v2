namespace Psycheflow.Api.Features.Sessions.RescheduleSession;

/// <param name="DurationMinutes">Opcional: mantém a duração atual da sessão quando omitido.</param>
/// <param name="Reason">Obrigatório (RN-42).</param>
public sealed record RescheduleSessionRequest(DateOnly? Date, TimeOnly? StartTime, string? Reason, int? DurationMinutes = null);

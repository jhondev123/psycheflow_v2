namespace Psycheflow.Api.Features.Sessions.ListSessions;

/// <param name="From">Início do período (obrigatório, RF005).</param>
/// <param name="To">Fim do período (opcional).</param>
/// <param name="TimeFrom">Sessões que começam a partir deste horário.</param>
/// <param name="TimeTo">Sessões que começam até este horário.</param>
/// <param name="PsychologistId">Psicólogo: o próprio por padrão; Admin/Manager veem todos se omitido.</param>
public sealed record ListSessionsQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    TimeOnly? TimeFrom = null,
    TimeOnly? TimeTo = null,
    Guid? PatientId = null,
    Guid? PsychologistId = null,
    SessionStatus? Status = null,
    int? Page = null,
    int? PageSize = null);

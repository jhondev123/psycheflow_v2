namespace Psycheflow.Api.Features.Recurrences.CreateRecurrence;

/// <param name="StartDate">Primeira sessão; define o dia da semana (semanal) ou do mês (mensal).</param>
/// <param name="EndDate">Última data possível (opcional). Sem ela, gera janelas de 3 meses sob demanda.</param>
/// <param name="DurationMinutes">Padrão: duração configurada na empresa.</param>
/// <param name="Price">Valor de cada sessão; padrão: valor configurado na empresa.</param>
/// <param name="PsychologistId">Opcional para o psicólogo (usa o próprio).</param>
public sealed record CreateRecurrenceRequest(
    Guid PatientId,
    RecurrenceType? Type,
    DateOnly? StartDate,
    TimeOnly? StartTime,
    DateOnly? EndDate = null,
    int? DurationMinutes = null,
    decimal? Price = null,
    Guid? PsychologistId = null);

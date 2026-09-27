namespace Psycheflow.Api.Features.Sessions.CreateSession;

/// <param name="PsychologistId">Opcional para o psicólogo (usa o próprio); obrigatório para Admin/Manager sem perfil.</param>
/// <param name="DurationMinutes">Opcional: se omitido, usa a duração padrão da empresa (RN-32).</param>
/// <param name="Notes">Anotações prévias em Markdown (opcional).</param>
/// <param name="Price">Valor da sessão; padrão = valor da empresa (RN-51).</param>
public sealed record CreateSessionRequest(
    Guid PatientId,
    DateOnly? Date,
    TimeOnly? StartTime,
    int? DurationMinutes = null,
    Guid? PsychologistId = null,
    string? Notes = null,
    decimal? Price = null);

namespace Psycheflow.Api.Features.Scheduling.GetAgenda;

/// <param name="PsychologistId">
/// Psicólogo: omitido = a própria agenda. Admin/Manager: omitido = todos os psicólogos da empresa.
/// </param>
public sealed record AgendaQuery(DateOnly? From = null, DateOnly? To = null, Guid? PsychologistId = null)
{
    public const int MaxDays = 62;
}

namespace Psycheflow.Api.Features.Scheduling.CreateBlock;

/// <summary>
/// RF018 / UC19: bloqueio de agenda. Sem horários = dias inteiros de <paramref name="StartDate"/> a <paramref name="EndDate"/>;
/// com horários = a mesma faixa em cada dia do intervalo.
/// </summary>
/// <param name="EndDate">Opcional: último dia (padrão: o mesmo da data inicial).</param>
/// <param name="StartTime">Informe junto com <paramref name="EndTime"/> para bloquear só uma faixa de horário.</param>
/// <param name="PsychologistId">Opcional para o psicólogo (usa o próprio).</param>
public sealed record CreateBlockRequest(
    DateOnly? StartDate,
    DateOnly? EndDate = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Reason = null,
    Guid? PsychologistId = null)
{
    public const int MaxDays = 31;

    public DateOnly LastDate => EndDate ?? StartDate!.Value;

    public bool IsWholeDay => StartTime is null && EndTime is null;
}

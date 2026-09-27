namespace Psycheflow.Api.Features.Psychologists.SetWorkingHours;

/// <param name="Hours">Lista completa de faixas; substitui o expediente atual (lista vazia limpa tudo).</param>
public sealed record SetWorkingHoursRequest(IReadOnlyList<WorkingHoursDto> Hours);

using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Psychologists;

public static class PsychologistErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "psychologist.not_found", "Psicólogo não encontrado.");

    public static readonly Error ProfileNotFound = Error.NotFound(
        "psychologist.profile_not_found", "O usuário logado não possui perfil de psicólogo.");

    public static readonly Error CannotManage = Error.Forbidden(
        "psychologist.cannot_manage", "Você só pode alterar o próprio perfil e a própria agenda.");

    public static readonly Error InvalidWorkingHoursRange = Error.Validation(
        "psychologist.invalid_working_hours", "O horário final de cada faixa deve ser depois do inicial.", "hours");

    public static readonly Error OverlappingWorkingHours = Error.Validation(
        "psychologist.overlapping_working_hours", "Há faixas de horário sobrepostas no mesmo dia.", "hours");
}

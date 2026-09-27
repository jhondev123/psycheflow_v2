using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Scheduling;

public static class SchedulingErrors
{
    public static readonly Error InvalidRange = Error.Validation(
        "scheduling.invalid_range", "O horário final deve ser depois do horário inicial.", "endTime");

    public static readonly Error CrossesMidnight = Error.Validation(
        "scheduling.crosses_midnight", "O atendimento precisa terminar no mesmo dia.", "durationMinutes");

    public static readonly Error InThePast = Error.Validation(
        "scheduling.in_the_past", "Não é possível agendar em uma data ou horário que já passou.", "startTime");

    public static readonly Error OutsideWorkingHours = Error.Validation(
        "scheduling.outside_working_hours", "O horário está fora do expediente do psicólogo.", "startTime");

    public static readonly Error Conflict = Error.Conflict(
        "scheduling.conflict", "Já existe um agendamento ou bloqueio nesse horário.");

    public static readonly Error PsychologistRequired = Error.Validation(
        "scheduling.psychologist_required", "Informe o psicólogo da agenda.", "psychologistId");

    public static readonly Error CannotManageAgenda = Error.Forbidden(
        "scheduling.cannot_manage_agenda", "Você só pode acessar a própria agenda.");

    public static readonly Error BlockNotFound = Error.NotFound(
        "scheduling.block_not_found", "Bloqueio não encontrado.");
}

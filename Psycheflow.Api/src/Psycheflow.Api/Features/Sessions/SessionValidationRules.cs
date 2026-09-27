using FluentValidation;
using Psycheflow.Api.Features.Companies;

namespace Psycheflow.Api.Features.Sessions;

internal static class SessionValidationRules
{
    /// <summary>RN-32: duração opcional, mas quando informada fica entre 15 e 240 minutos.</summary>
    public static IRuleBuilderOptions<T, int?> ValidDuration<T>(this IRuleBuilder<T, int?> rule) =>
        rule
            .InclusiveBetween(CompanySettings.MinSessionDurationMinutes, CompanySettings.MaxSessionDurationMinutes)
            .WithMessage($"A duração deve ficar entre {CompanySettings.MinSessionDurationMinutes} e {CompanySettings.MaxSessionDurationMinutes} minutos.");

    public static IRuleBuilderOptions<T, string?> RequiredReason<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .NotEmpty().WithMessage(SessionErrors.ReasonRequired.Message)
            .MaximumLength(Session.ReasonMaxLength).WithMessage($"Use no máximo {Session.ReasonMaxLength} caracteres.");
}

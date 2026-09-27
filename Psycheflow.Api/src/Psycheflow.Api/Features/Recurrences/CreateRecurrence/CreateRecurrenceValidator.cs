using FluentValidation;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Recurrences.CreateRecurrence;

public sealed class CreateRecurrenceValidator : AbstractValidator<CreateRecurrenceRequest>
{
    public CreateRecurrenceValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Informe o paciente.");
        RuleFor(x => x.Type).NotNull().WithMessage("Informe o tipo (semanal ou mensal).").IsInEnum().WithMessage("Tipo inválido.");
        RuleFor(x => x.StartDate).NotNull().WithMessage("Informe a data da primeira sessão.");
        RuleFor(x => x.StartTime).NotNull().WithMessage("Informe o horário.");
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("A data final deve ser igual ou posterior à inicial.")
            .When(x => x.EndDate is not null && x.StartDate is not null);
        RuleFor(x => x.DurationMinutes).ValidDuration();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("O valor não pode ser negativo.");
    }
}

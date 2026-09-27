using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Psychologists.SetWorkingHours;

public sealed class SetWorkingHoursValidator : AbstractValidator<SetWorkingHoursRequest>
{
    public SetWorkingHoursValidator()
    {
        RuleFor(x => x.Hours).NotNull().WithMessage("Informe a lista de horários (pode ser vazia).");
        RuleForEach(x => x.Hours).ChildRules(range =>
        {
            range.RuleFor(r => r.DayOfWeek).DefinedEnum();
            range.RuleFor(r => r.EndTime)
                .GreaterThan(r => r.StartTime)
                .WithMessage(PsychologistErrors.InvalidWorkingHoursRange.Message);
        });
    }
}

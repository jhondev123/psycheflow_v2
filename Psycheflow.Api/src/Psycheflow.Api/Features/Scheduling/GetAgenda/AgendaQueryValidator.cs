using FluentValidation;

namespace Psycheflow.Api.Features.Scheduling.GetAgenda;

public sealed class AgendaQueryValidator : AbstractValidator<AgendaQuery>
{
    public AgendaQueryValidator()
    {
        RuleFor(x => x.From).NotNull().WithMessage("Informe a data inicial.");
        RuleFor(x => x.To).NotNull().WithMessage("Informe a data final.");

        When(x => x.From is not null && x.To is not null, () =>
            RuleFor(x => x.To)
                .GreaterThanOrEqualTo(x => x.From).WithMessage("A data final deve ser igual ou posterior à inicial.")
                .Must((query, to) => to!.Value.DayNumber - query.From!.Value.DayNumber < AgendaQuery.MaxDays)
                .WithMessage($"Consulte no máximo {AgendaQuery.MaxDays} dias por vez."));
    }
}

using FluentValidation;

namespace Psycheflow.Api.Features.Sessions.ListSessions;

public sealed class ListSessionsValidator : AbstractValidator<ListSessionsQuery>
{
    public ListSessionsValidator()
    {
        RuleFor(x => x.From).NotNull().WithMessage("Informe a data inicial do período.");
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).WithMessage("A data final deve ser igual ou posterior à inicial.")
            .When(x => x.To is not null && x.From is not null);
        RuleFor(x => x.TimeTo)
            .GreaterThanOrEqualTo(x => x.TimeFrom).WithMessage("O horário final deve ser igual ou posterior ao inicial.")
            .When(x => x.TimeTo is not null && x.TimeFrom is not null);
    }
}

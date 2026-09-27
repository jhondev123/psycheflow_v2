using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Scheduling.CreateBlock;

public sealed class CreateBlockValidator : AbstractValidator<CreateBlockRequest>
{
    public CreateBlockValidator()
    {
        RuleFor(x => x.StartDate).NotNull().WithMessage("Informe a data inicial.");

        When(x => x.StartDate is not null && x.EndDate is not null, () =>
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("A data final deve ser igual ou posterior à inicial.")
                .Must((request, endDate) => endDate!.Value.DayNumber - request.StartDate!.Value.DayNumber < CreateBlockRequest.MaxDays)
                .WithMessage($"Bloqueie no máximo {CreateBlockRequest.MaxDays} dias por vez."));

        RuleFor(x => x.StartTime)
            .NotNull().WithMessage("Informe também o horário inicial.")
            .When(x => x.EndTime is not null);

        RuleFor(x => x.EndTime)
            .NotNull().WithMessage("Informe também o horário final.")
            .GreaterThan(x => x.StartTime).WithMessage(SchedulingErrors.InvalidRange.Message)
            .When(x => x.StartTime is not null);

        RuleFor(x => x.Reason).OptionalText(Schedule.ReasonMaxLength);
    }
}

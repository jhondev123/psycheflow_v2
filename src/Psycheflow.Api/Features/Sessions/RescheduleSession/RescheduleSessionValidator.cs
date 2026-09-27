using FluentValidation;

namespace Psycheflow.Api.Features.Sessions.RescheduleSession;

public sealed class RescheduleSessionValidator : AbstractValidator<RescheduleSessionRequest>
{
    public RescheduleSessionValidator()
    {
        RuleFor(x => x.Date).NotNull().WithMessage("Informe a nova data.");
        RuleFor(x => x.StartTime).NotNull().WithMessage("Informe o novo horário.");
        RuleFor(x => x.Reason).RequiredReason();
        RuleFor(x => x.DurationMinutes).ValidDuration();
    }
}

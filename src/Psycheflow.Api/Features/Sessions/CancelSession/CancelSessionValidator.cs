using FluentValidation;

namespace Psycheflow.Api.Features.Sessions.CancelSession;

public sealed class CancelSessionValidator : AbstractValidator<CancelSessionRequest>
{
    public CancelSessionValidator() => RuleFor(x => x.Reason).RequiredReason();
}

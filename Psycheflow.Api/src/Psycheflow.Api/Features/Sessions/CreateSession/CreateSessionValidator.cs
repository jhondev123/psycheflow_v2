using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Sessions.CreateSession;

public sealed class CreateSessionValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Informe o paciente.");
        RuleFor(x => x.Date).NotNull().WithMessage("Informe a data.");
        RuleFor(x => x.StartTime).NotNull().WithMessage("Informe o horário.");
        RuleFor(x => x.DurationMinutes).ValidDuration();
        RuleFor(x => x.Notes).OptionalText(Session.NotesMaxLength);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("O valor não pode ser negativo.");
    }
}

using FluentValidation;
using Psycheflow.Api.Common.Validation;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Ai.Suggestions.SessionNotes;

public sealed class SuggestSessionNotesValidator : AbstractValidator<SuggestSessionNotesRequest>
{
    public SuggestSessionNotesValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Informe a sessão.");
        RuleFor(x => x.Draft).OptionalText(Session.NotesMaxLength);
    }
}

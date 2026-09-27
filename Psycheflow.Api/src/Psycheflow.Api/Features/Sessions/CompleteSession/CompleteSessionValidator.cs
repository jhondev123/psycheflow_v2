using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Sessions.CompleteSession;

public sealed class CompleteSessionValidator : AbstractValidator<CompleteSessionRequest>
{
    public CompleteSessionValidator()
    {
        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage(SessionErrors.NotesRequired.Message)
            .MaximumLength(Session.NotesMaxLength).WithMessage($"Use no máximo {Session.NotesMaxLength} caracteres.");
        RuleFor(x => x.FeedbackScore)
            .NotNull().WithMessage(SessionErrors.InvalidFeedbackScore.Message)
            .InclusiveBetween(Session.MinFeedbackScore, Session.MaxFeedbackScore).WithMessage(SessionErrors.InvalidFeedbackScore.Message);
        RuleFor(x => x.FeedbackComment).OptionalText(Session.CommentMaxLength);
    }
}

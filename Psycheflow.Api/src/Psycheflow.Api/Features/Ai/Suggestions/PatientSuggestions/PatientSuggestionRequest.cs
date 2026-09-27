using FluentValidation;

namespace Psycheflow.Api.Features.Ai.Suggestions.PatientSuggestions;

public sealed record PatientSuggestionRequest(Guid PatientId);

public sealed class PatientSuggestionValidator : AbstractValidator<PatientSuggestionRequest>
{
    public PatientSuggestionValidator() =>
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Informe o paciente.");
}

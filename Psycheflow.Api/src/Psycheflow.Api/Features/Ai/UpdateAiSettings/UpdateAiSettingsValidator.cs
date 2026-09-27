using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Ai.UpdateAiSettings;

public sealed class UpdateAiSettingsValidator : AbstractValidator<UpdateAiSettingsRequest>
{
    public UpdateAiSettingsValidator()
    {
        RuleFor(x => x.Provider).DefinedEnum();

        RuleFor(x => x.AcceptTerms)
            .Equal(true).When(x => x.IsEnabled)
            .WithMessage("Para habilitar a IA é preciso aceitar os termos de uso e o envio dos dados pseudonimizados.");
    }
}

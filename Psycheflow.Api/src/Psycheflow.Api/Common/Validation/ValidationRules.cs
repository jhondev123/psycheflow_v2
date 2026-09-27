using FluentValidation;
using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Common.Validation;

/// <summary>Regras de formato reaproveitadas pelos validators dos slices (mensagens em pt-BR).</summary>
public static class ValidationRules
{
    public const int EmailMaxLength = 256;

    public static IRuleBuilderOptions<T, string?> RequiredText<T>(this IRuleBuilder<T, string?> rule, string label, int maxLength) =>
        rule
            .NotEmpty().WithMessage($"Informe {label}.")
            .MaximumLength(maxLength).WithMessage($"Use no máximo {maxLength} caracteres.");

    public static IRuleBuilderOptions<T, string?> OptionalText<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule.MaximumLength(maxLength).WithMessage($"Use no máximo {maxLength} caracteres.");

    public static IRuleBuilderOptions<T, string?> ValidEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .NotEmpty().WithMessage("Informe o e-mail.")
            .MaximumLength(EmailMaxLength).WithMessage($"Use no máximo {EmailMaxLength} caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");

    public static IRuleBuilderOptions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(Phone.IsValid).WithMessage(Phone.Invalid.Message);

    public static IRuleBuilderOptions<T, TEnum> DefinedEnum<T, TEnum>(this IRuleBuilder<T, TEnum> rule)
        where TEnum : struct, Enum =>
        rule.IsInEnum().WithMessage("Valor inválido.");
}

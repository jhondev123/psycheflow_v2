using FluentValidation;

namespace Psycheflow.Api.Features.Auth.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Informe a senha atual.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Informe a nova senha.")
            .NotEqual(x => x.CurrentPassword).WithMessage("A nova senha deve ser diferente da atual.");
    }
}

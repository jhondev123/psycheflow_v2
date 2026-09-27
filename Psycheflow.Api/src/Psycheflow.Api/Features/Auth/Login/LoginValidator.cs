using FluentValidation;

namespace Psycheflow.Api.Features.Auth.Login;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe o e-mail.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Informe a senha.");
    }
}

using FluentValidation;
using Psycheflow.Api.Common.Validation;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Auth.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.CompanyName).RequiredText("o nome da clínica", Company.NameMaxLength);
        RuleFor(x => x.FullName).RequiredText("o seu nome", User.FullNameMaxLength);
        RuleFor(x => x.Email).ValidEmail();

        // A força da senha (RN-11) é validada pelo ASP.NET Identity, que é a fonte da política.
        RuleFor(x => x.Password).NotEmpty().WithMessage("Informe a senha.");

        RuleFor(x => x.LicenseNumber).Must(LicenseNumber.IsValid).WithMessage(LicenseNumber.Invalid.Message);
        RuleFor(x => x.Approach).DefinedEnum();
        RuleFor(x => x.Phone).ValidPhone().When(x => x.Phone is not null);
    }
}

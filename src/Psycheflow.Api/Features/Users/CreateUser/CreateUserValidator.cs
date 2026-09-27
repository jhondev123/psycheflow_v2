using FluentValidation;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Validation;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Users.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FullName).RequiredText("o nome", User.FullNameMaxLength);
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Role)
            .Must(role => Roles.Assignable.Contains(role))
            .WithMessage($"Perfil inválido. Use um destes: {string.Join(", ", Roles.Assignable)}.");

        RuleFor(x => x.LicenseNumber)
            .Must(LicenseNumber.IsValid).WithMessage(LicenseNumber.Invalid.Message)
            .When(x => x.Role == Roles.Psychologist);

        RuleFor(x => x.Approach!.Value).DefinedEnum().When(x => x.Approach is not null);
    }
}

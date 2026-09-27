using FluentValidation;
using Psycheflow.Api.Common.Validation;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Psychologists.UpdateProfile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName).RequiredText("o nome", User.FullNameMaxLength);
        RuleFor(x => x.LicenseNumber).Must(LicenseNumber.IsValid).WithMessage(LicenseNumber.Invalid.Message);
        RuleFor(x => x.Approach).DefinedEnum();
        RuleFor(x => x.Phone).ValidPhone().When(x => x.Phone is not null);
    }
}

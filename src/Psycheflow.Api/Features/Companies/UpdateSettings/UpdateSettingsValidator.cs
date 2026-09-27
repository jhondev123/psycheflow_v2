using FluentValidation;
using Psycheflow.Api.Common.Time;

namespace Psycheflow.Api.Features.Companies.UpdateSettings;

public sealed class UpdateSettingsValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsValidator()
    {
        RuleFor(x => x.SessionDurationMinutes)
            .InclusiveBetween(CompanySettings.MinSessionDurationMinutes, CompanySettings.MaxSessionDurationMinutes)
            .WithMessage(CompanySettingsErrors.InvalidSessionDuration.Message);

        RuleFor(x => x.SessionDefaultPrice)
            .GreaterThanOrEqualTo(0).WithMessage(CompanySettingsErrors.InvalidPrice.Message);

        RuleFor(x => x.TimeZone)
            .Must(ClinicClock.IsValidTimeZone).WithMessage(CompanySettingsErrors.InvalidTimeZone.Message);
    }
}

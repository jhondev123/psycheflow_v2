using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Patients.UpdatePatient;

public sealed class UpdatePatientValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientValidator(TimeProvider timeProvider)
    {
        Include(new PatientDataValidator(timeProvider));
        RuleFor(x => x.Status).DefinedEnum();
    }
}

using FluentValidation;

namespace Psycheflow.Api.Features.Patients.CreatePatient;

public sealed class CreatePatientValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientValidator(TimeProvider timeProvider)
    {
        Include(new PatientDataValidator(timeProvider));
        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("Informe o CPF.")
            .Must(Cpf.IsValid).WithMessage(Cpf.Invalid.Message);
    }
}

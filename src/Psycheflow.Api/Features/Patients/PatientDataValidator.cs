using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Patients;

/// <summary>Dados pessoais editáveis — comuns ao cadastro e à edição do paciente.</summary>
public interface IPatientData
{
    string FullName { get; }

    string Email { get; }

    string Phone { get; }

    DateOnly? BirthDate { get; }

    AddressDto? Address { get; }

    string? Notes { get; }
}

/// <summary>RN-20, RN-23, RN-24: obrigatórios e formatos. Incluído pelos validators de cadastro e edição.</summary>
public sealed class PatientDataValidator : AbstractValidator<IPatientData>
{
    private static readonly DateOnly MinBirthDate = new(1900, 1, 1);

    public PatientDataValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.FullName).RequiredText("o nome", Patient.FullNameMaxLength);
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Informe o telefone.").ValidPhone();

        RuleFor(x => x.BirthDate)
            .Must(date => date >= MinBirthDate && date <= DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("Data de nascimento inválida.")
            .When(x => x.BirthDate is not null);

        RuleFor(x => x.Address!).SetValidator(new AddressValidator()).When(x => x.Address is not null);
        RuleFor(x => x.Notes).OptionalText(Patient.NotesMaxLength);
    }
}

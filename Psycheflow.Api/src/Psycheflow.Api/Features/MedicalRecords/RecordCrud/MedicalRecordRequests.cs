using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.MedicalRecords.RecordCrud;

public sealed record CreateMedicalRecordRequest(Guid PatientId, string? Title, string? Content);

public sealed record UpdateMedicalRecordRequest(string? Title, string? Content);

/// <param name="Search">Palavra-chave no título ou no conteúdo.</param>
/// <param name="From">Registros criados a partir desta data (fuso da clínica).</param>
/// <param name="To">Registros criados até esta data (fuso da clínica).</param>
public sealed record ListMedicalRecordsQuery(
    Guid? PatientId = null,
    string? Search = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int? Page = null,
    int? PageSize = null);

public sealed class CreateMedicalRecordValidator : AbstractValidator<CreateMedicalRecordRequest>
{
    public CreateMedicalRecordValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Informe o paciente.");
        RuleFor(x => x.Title).RequiredText("o título", MedicalRecord.TitleMaxLength);
        RuleFor(x => x.Content).RequiredText("o conteúdo do registro", MedicalRecord.ContentMaxLength);
    }
}

public sealed class UpdateMedicalRecordValidator : AbstractValidator<UpdateMedicalRecordRequest>
{
    public UpdateMedicalRecordValidator()
    {
        RuleFor(x => x.Title).RequiredText("o título", MedicalRecord.TitleMaxLength);
        RuleFor(x => x.Content).RequiredText("o conteúdo do registro", MedicalRecord.ContentMaxLength);
    }
}

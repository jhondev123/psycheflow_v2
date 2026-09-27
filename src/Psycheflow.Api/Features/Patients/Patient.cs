using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Patients;

/// <summary>
/// Paciente da empresa (RF001–RF003). Visível a todos os profissionais da empresa (D-02).
/// Não tem login hoje; <see cref="UserId"/> fica reservado para um futuro portal do paciente (D-01).
/// </summary>
public sealed class Patient : Entity, ITenantEntity, ISoftDeletable
{
    public const int FullNameMaxLength = 150;
    public const int EmailMaxLength = 256;
    public const int NotesMaxLength = 2000;

    private Patient()
    {
    }

    public Guid CompanyId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    /// <summary>Não editável depois do cadastro (RN-27).</summary>
    public Cpf Cpf { get; private set; } = null!;

    public string Email { get; private set; } = string.Empty;

    public Phone Phone { get; private set; } = null!;

    public DateOnly? BirthDate { get; private set; }

    public PatientStatus Status { get; private set; }

    public Address? Address { get; private set; }

    public string? Notes { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>RN-25: nasce ativo. A empresa é preenchida automaticamente com a do usuário logado.</summary>
    public static Patient Create(
        string fullName, Cpf cpf, string email, Phone phone, DateOnly? birthDate, Address? address, string? notes)
    {
        var patient = new Patient { Cpf = cpf, Status = PatientStatus.Active };
        patient.SetData(fullName, email, phone, birthDate, address, notes);
        return patient;
    }

    public void Update(
        string fullName, string email, Phone phone, DateOnly? birthDate, PatientStatus status, Address? address, string? notes)
    {
        SetData(fullName, email, phone, birthDate, address, notes);
        Status = status;
    }

    private void SetData(string fullName, string email, Phone phone, DateOnly? birthDate, Address? address, string? notes)
    {
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone;
        BirthDate = birthDate;
        Address = address;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}

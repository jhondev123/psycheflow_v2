using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Companies;

/// <summary>Empresa (clínica ou psicólogo autônomo). É o tenant: todo dado de negócio pertence a uma.</summary>
public sealed class Company : Entity
{
    public const int NameMaxLength = 150;

    private Company()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public CompanySettings Settings { get; private set; } = CompanySettings.Default();

    public static Result<Company> Create(string name)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > NameMaxLength)
        {
            return CompanyErrors.InvalidName;
        }

        return new Company { Name = trimmed };
    }
}

public static class CompanyErrors
{
    public static readonly Error InvalidName = Error.Validation(
        "company.invalid_name", $"Informe o nome da empresa (até {Company.NameMaxLength} caracteres).", "companyName");
}

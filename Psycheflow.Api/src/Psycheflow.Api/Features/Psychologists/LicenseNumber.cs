using System.Text.RegularExpressions;
using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Psychologists;

/// <summary>Registro no Conselho Regional de Psicologia (CRP): região/número, ex.: 06/12345 (D-03).</summary>
public sealed partial record LicenseNumber
{
    public const int MaxLength = 9;

    public static readonly Error Invalid = Error.Validation(
        "psychologist.invalid_license_number", "CRP inválido. Use o formato região/número, ex.: 06/12345.", "licenseNumber");

    private LicenseNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<LicenseNumber> Create(string? input)
    {
        string value = input?.Trim() ?? string.Empty;
        return Pattern().IsMatch(value) ? new LicenseNumber(value) : Invalid;
    }

    public static bool IsValid(string? input) => Create(input).IsSuccess;

    internal static LicenseNumber FromDatabase(string value) => new(value);

    public override string ToString() => Value;

    [GeneratedRegex(@"^\d{2}/\d{4,6}$")]
    private static partial Regex Pattern();
}

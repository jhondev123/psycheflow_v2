namespace Psycheflow.Api.Common.Domain;

/// <summary>Telefone brasileiro (DDD + 8 ou 9 dígitos). Guardado só com dígitos; a máscara é responsabilidade do front.</summary>
public sealed record Phone
{
    public static readonly Error Invalid = Error.Validation(
        "phone.invalid", "Telefone inválido. Informe DDD + número, ex.: (45) 99999-1234.", "phone");

    private const string AllowedSeparators = "()- +";

    private Phone(string value) => Value = value;

    public string Value { get; }

    public static Result<Phone> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Any(c => !char.IsAsciiDigit(c) && !AllowedSeparators.Contains(c)))
        {
            return Invalid;
        }

        string digits = new(input.Where(char.IsAsciiDigit).ToArray());
        bool validLength = digits.Length is 10 or 11;
        bool validAreaCode = validLength && digits[0] != '0' && digits[1] != '0';

        return validLength && validAreaCode ? new Phone(digits) : Invalid;
    }

    public static bool IsValid(string? input) => Create(input).IsSuccess;

    internal static Phone FromDatabase(string value) => new(value);

    public override string ToString() => Value;
}

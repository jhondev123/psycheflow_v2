using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Patients;

/// <summary>CPF (RN-21): 11 dígitos, dígitos verificadores corretos e não todos iguais. Guardado só com dígitos.</summary>
public sealed record Cpf
{
    public const int Length = 11;

    public static readonly Error Invalid = Error.Validation("patient.invalid_cpf", "CPF inválido.", "cpf");

    private Cpf(string value) => Value = value;

    public string Value { get; }

    public static Result<Cpf> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Any(c => !char.IsAsciiDigit(c) && c is not '.' and not '-' and not ' '))
        {
            return Invalid;
        }

        string digits = new(input.Where(char.IsAsciiDigit).ToArray());
        return HasValidDigits(digits) ? new Cpf(digits) : Invalid;
    }

    public static bool IsValid(string? input) => Create(input).IsSuccess;

    internal static Cpf FromDatabase(string value) => new(value);

    public override string ToString() => Value;

    private static bool HasValidDigits(string digits)
    {
        if (digits.Length != Length || digits.All(d => d == digits[0]))
        {
            return false;
        }

        return digits[9] - '0' == CheckDigit(digits, 9) && digits[10] - '0' == CheckDigit(digits, 10);
    }

    private static int CheckDigit(string digits, int length)
    {
        int sum = 0;
        for (int i = 0; i < length; i++)
        {
            sum += (digits[i] - '0') * (length + 1 - i);
        }

        int remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}

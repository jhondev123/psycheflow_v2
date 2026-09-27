using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Psycheflow.Api.Features.Ai.Assistant;

/// <summary>Dados que identificam o paciente e nunca podem sair para o provedor de IA.</summary>
public sealed record PatientIdentity(string FullName, string? Cpf, string? Email, string? Phone, string? Street);

/// <summary>
/// Pseudonimização (LGPD, art. 13) aplicada a todo texto enviado à IA: o nome do paciente (completo ou partes, sem diferenciar
/// maiúsculas/acentos) vira "[paciente]"; prenomes comuns de outras pessoas citadas (familiares, colegas) viram "[pessoa]";
/// CPF, e-mail, telefone, CEP e rua são mascarados.
/// Limitação: terceiros com nomes fora da lista de prenomes comuns (ou só pelo sobrenome) não são detectados.
/// </summary>
public static partial class Pseudonymizer
{
    public const string PatientPlaceholder = "[paciente]";

    public const string OtherPersonPlaceholder = "[pessoa]";

    private const int MinNamePartLength = 3;
    private const int MinStreetLength = 5;
    private const int RegexTimeoutMilliseconds = 1000;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(RegexTimeoutMilliseconds);

    private static readonly HashSet<string> NameParticles = new(StringComparer.OrdinalIgnoreCase) { "das", "dos", "del", "von", "van" };

    public static string Apply(string text, PatientIdentity identity)
    {
        string result = text;

        // Primeiro os dados do próprio paciente (aceitando outra formatação), depois os padrões genéricos.
        result = ReplaceDigits(result, identity.Cpf, "[cpf]");
        result = ReplaceDigits(result, identity.Phone, "[telefone]");
        if (!string.IsNullOrWhiteSpace(identity.Email))
        {
            result = Regex.Replace(result, Regex.Escape(identity.Email), "[email]", RegexOptions.IgnoreCase, RegexTimeout);
        }

        result = EmailPattern().Replace(result, "[email]");
        result = CpfPattern().Replace(result, "[cpf]");
        result = ZipCodePattern().Replace(result, "[cep]");
        result = PhonePattern().Replace(result, "[telefone]");

        if (identity.Street is { Length: >= MinStreetLength } street)
        {
            result = ReplaceWords(result, street, "[endereço]");
        }

        result = ReplaceWords(result, identity.FullName, PatientPlaceholder);
        foreach (string part in NameParts(identity.FullName))
        {
            result = ReplaceWords(result, part, PatientPlaceholder);
        }

        return CapitalizedWordPattern().Replace(result, match =>
            CommonFirstNames.All.Contains(RemoveDiacritics(match.Value).ToLowerInvariant()) ? OtherPersonPlaceholder : match.Value);
    }

    private static IEnumerable<string> NameParts(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length >= MinNamePartLength && !NameParticles.Contains(part))
            .OrderByDescending(part => part.Length);

    /// <summary>Troca o termo apenas como palavra inteira, ignorando maiúsculas e acentos ("Júlia" = "JULIA").</summary>
    private static string ReplaceWords(string text, string term, string replacement)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return text;
        }

        string pattern = $@"(?<!\w){AccentInsensitive(term.Trim())}(?!\w)";
        return Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
    }

    /// <summary>Encontra a sequência de dígitos mesmo com pontuação ou espaços entre eles ("529 982 247-25").</summary>
    private static string ReplaceDigits(string text, string? digits, string replacement)
    {
        if (string.IsNullOrWhiteSpace(digits))
        {
            return text;
        }

        string pattern = $@"(?<!\d)\(?{string.Join(@"[\s.\-()]*", digits.Where(char.IsAsciiDigit))}(?!\d)";
        return Regex.Replace(text, pattern, replacement, RegexOptions.None, RegexTimeout);
    }

    private static string AccentInsensitive(string term)
    {
        var pattern = new StringBuilder();
        foreach (char c in RemoveDiacritics(term))
        {
            pattern.Append(char.ToLowerInvariant(c) switch
            {
                'a' => "[aáàâãä]",
                'e' => "[eéèêë]",
                'i' => "[iíìîï]",
                'o' => "[oóòôõö]",
                'u' => "[uúùûü]",
                'c' => "[cç]",
                'n' => "[nñ]",
                _ when char.IsWhiteSpace(c) => @"\s+",
                _ => Regex.Escape(c.ToString()),
            });
        }

        return pattern.ToString();
    }

    private static string RemoveDiacritics(string value)
    {
        string decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Palavra iniciada por maiúscula, exceto em nomes de lugares como "São Paulo" ou "Santa Maria".</summary>
    [GeneratedRegex(@"(?<!\b(?:S[ãa]o|Santa|Santo)\s)(?<!\w)\p{Lu}\p{Ll}+(?!\w)", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex CapitalizedWordPattern();

    [GeneratedRegex(@"[\w.%+-]+@[\w-]+(?:\.[\w-]+)+", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(?<!\w)\d{3}\.?\d{3}\.?\d{3}-?\d{2}(?!\w)", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex CpfPattern();

    [GeneratedRegex(@"(?<!\w)\d{5}-\d{3}(?!\w)", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex ZipCodePattern();

    [GeneratedRegex(@"(?<!\w)(?:\+?55\s?)?\(?\d{2}\)?\s?9?\d{4}[-\s]?\d{4}(?!\w)", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex PhonePattern();
}

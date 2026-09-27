namespace Psycheflow.Api.Features.Documents.Pdf;

/// <summary>Valor monetário por extenso em português (ex.: 180,50 → "cento e oitenta reais e cinquenta centavos").</summary>
public static class AmountInWords
{
    private static readonly string[] Units =
    [
        "zero", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove", "dez",
        "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove",
    ];

    private static readonly string[] Tens =
        ["", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"];

    private static readonly string[] Hundreds =
        ["", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos"];

    public static string ToPortuguese(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        long reais = (long)decimal.Truncate(amount);
        int cents = (int)decimal.Round((amount - reais) * 100, MidpointRounding.AwayFromZero);

        if (reais == 0 && cents == 0)
        {
            return "zero reais";
        }

        List<string> parts = [];
        if (reais > 0)
        {
            string currency = reais == 1 ? "real" : "reais";
            bool exactMillions = reais >= 1_000_000 && reais % 1_000_000 == 0;
            parts.Add($"{Number(reais)} {(exactMillions ? "de reais" : currency)}");
        }

        if (cents > 0)
        {
            parts.Add($"{Number(cents)} {(cents == 1 ? "centavo" : "centavos")}");
        }

        return string.Join(" e ", parts);
    }

    private static string Number(long value)
    {
        long millions = value / 1_000_000;
        long thousands = value / 1000 % 1000;
        long rest = value % 1000;

        List<(long Value, string Text)> groups = [];
        if (millions > 0)
        {
            groups.Add((millions * 1_000_000, millions == 1 ? "um milhão" : $"{Group(millions)} milhões"));
        }

        if (thousands > 0)
        {
            groups.Add((thousands * 1000, thousands == 1 ? "mil" : $"{Group(thousands)} mil"));
        }

        if (rest > 0)
        {
            groups.Add((rest, Group(rest)));
        }

        if (groups.Count == 0)
        {
            return Units[0];
        }

        // "mil e um", "mil e duzentos", mas "mil duzentos e cinquenta".
        string text = groups[0].Text;
        for (int i = 1; i < groups.Count; i++)
        {
            long remaining = groups.Skip(i).Sum(g => g.Value);
            string connector = remaining < 100 || remaining % 100 == 0 ? " e " : " ";
            text += connector + groups[i].Text;
        }

        return text;
    }

    private static string Group(long value)
    {
        if (value == 100)
        {
            return "cem";
        }

        List<string> parts = [];
        long hundreds = value / 100;
        long below100 = value % 100;

        if (hundreds > 0)
        {
            parts.Add(Hundreds[hundreds]);
        }

        if (below100 > 0)
        {
            parts.Add(below100 < 20
                ? Units[below100]
                : below100 % 10 == 0 ? Tens[below100 / 10] : $"{Tens[below100 / 10]} e {Units[below100 % 10]}");
        }

        return string.Join(" e ", parts);
    }
}

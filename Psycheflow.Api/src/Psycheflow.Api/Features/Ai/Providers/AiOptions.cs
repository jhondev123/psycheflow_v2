namespace Psycheflow.Api.Features.Ai.Providers;

/// <summary>
/// Seção "Ai" da configuração. As chaves vêm de variáveis de ambiente (ex.: <c>Ai__Claude__ApiKey</c>) e nunca do banco;
/// provedor sem chave não aparece para as clínicas.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public AiProviderOptions Claude { get; set; } = new() { Model = "claude-opus-5" };

    public AiProviderOptions OpenAi { get; set; } = new() { Model = "gpt-5" };

    public AiProviderOptions Gemini { get; set; } = new() { Model = "gemini-2.5-pro" };

    /// <summary>Limite de tokens da resposta (inclui o raciocínio interno dos modelos que "pensam").</summary>
    public int MaxOutputTokens { get; set; } = 16_000;

    /// <summary>Tempo máximo de espera por uma sugestão.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}

public sealed class AiProviderOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Model);
}

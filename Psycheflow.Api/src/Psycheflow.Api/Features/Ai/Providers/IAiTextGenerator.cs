namespace Psycheflow.Api.Features.Ai.Providers;

/// <summary>Prompt já pseudonimizado: instruções fixas (<paramref name="System"/>) e dados + tarefa (<paramref name="User"/>).</summary>
public sealed record AiPrompt(string System, string User);

/// <summary>Resposta do provedor. <paramref name="Refused"/> indica recusa por política do modelo (não é falha técnica).</summary>
public sealed record AiCompletion(string Text, string Model, bool Refused);

/// <summary>
/// Porta genérica para geração de texto. Cada provedor (Claude, OpenAI, Gemini) adapta o próprio SDK a este contrato;
/// o restante do módulo não conhece SDKs. Falhas técnicas (rede, autenticação, limite) são lançadas como exceção.
/// </summary>
public interface IAiTextGenerator
{
    AiProvider Provider { get; }

    /// <summary>Só fica disponível para as clínicas se a chave de API estiver configurada no servidor.</summary>
    bool IsConfigured { get; }

    Task<AiCompletion> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken);
}

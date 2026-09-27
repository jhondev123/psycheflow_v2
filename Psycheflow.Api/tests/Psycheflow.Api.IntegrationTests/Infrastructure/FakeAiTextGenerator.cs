using Psycheflow.Api.Features.Ai;
using Psycheflow.Api.Features.Ai.Providers;

namespace Psycheflow.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Substitui os provedores reais nos testes: é o único provedor "configurado" (Claude), guarda os prompts recebidos
/// (para verificar a pseudonimização) e pode simular recusa ou falha.
/// </summary>
public sealed class FakeAiTextGenerator : IAiTextGenerator
{
    public const string DefaultResponse = "## Sugestão\nTexto gerado pelo provedor de teste.";
    public const string Model = "fake-model";

    private readonly List<AiPrompt> _prompts = [];

    public AiProvider Provider => AiProvider.Claude;

    public bool IsConfigured => true;

    public IReadOnlyList<AiPrompt> Prompts => _prompts;

    public AiPrompt LastPrompt => _prompts.ShouldHaveSingleItem();

    public bool Refuse { get; set; }

    public bool Fail { get; set; }

    public Task<AiCompletion> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken)
    {
        _prompts.Add(prompt);

        if (Fail)
        {
            throw new HttpRequestException("Falha simulada do provedor de IA.");
        }

        return Task.FromResult(Refuse ? new AiCompletion(string.Empty, Model, Refused: true) : new AiCompletion(DefaultResponse, Model, Refused: false));
    }

    public void Reset()
    {
        _prompts.Clear();
        Refuse = false;
        Fail = false;
    }
}

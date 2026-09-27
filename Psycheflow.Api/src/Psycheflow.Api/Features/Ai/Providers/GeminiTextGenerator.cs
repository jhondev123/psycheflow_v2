using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace Psycheflow.Api.Features.Ai.Providers;

/// <summary>Gemini (Google) via SDK oficial Google.GenAI, generateContent.</summary>
public sealed class GeminiTextGenerator(HttpClient httpClient, IOptions<AiOptions> options) : IAiTextGenerator
{
    private readonly AiOptions _options = options.Value;

    public AiProvider Provider => AiProvider.Gemini;

    public bool IsConfigured => _options.Gemini.IsConfigured;

    public async Task<AiCompletion> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken)
    {
        using var client = new Client(
            apiKey: _options.Gemini.ApiKey,
            clientOptions: new ClientOptions { HttpClientFactory = () => httpClient });

        GenerateContentResponse response = await client.Models.GenerateContentAsync(
            _options.Gemini.Model,
            prompt.User,
            new GenerateContentConfig
            {
                SystemInstruction = new Content { Parts = [new Part { Text = prompt.System }] },
                MaxOutputTokens = _options.MaxOutputTokens,
            },
            cancellationToken);

        string model = response.ModelVersion ?? _options.Gemini.Model;
        string? text = response.Text;

        // Sem texto e com bloqueio do prompt ou da resposta: recusa por política de segurança.
        return string.IsNullOrWhiteSpace(text) && (response.PromptFeedback?.BlockReason is not null || response.Candidates is not { Count: > 0 })
            ? new AiCompletion(string.Empty, model, Refused: true)
            : new AiCompletion(text ?? string.Empty, model, Refused: false);
    }
}

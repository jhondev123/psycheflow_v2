using Anthropic;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Options;

namespace Psycheflow.Api.Features.Ai.Providers;

/// <summary>
/// Claude (Anthropic) via SDK oficial, Messages API. O <see cref="AnthropicClient"/> é criado uma vez por instância e
/// descartado com ela (ao ser descartado, ele também descarta o <see cref="HttpClient"/> recebido).
/// </summary>
public sealed class ClaudeTextGenerator(HttpClient httpClient, IOptions<AiOptions> options) : IAiTextGenerator, IDisposable
{
    /// <summary>Habilita <c>fallbacks: "default"</c>: se o modelo recusar por política, o próprio servidor refaz com o modelo recomendado.</summary>
    public const string ServerSideFallbackBeta = "server-side-fallback-2026-07-01";

    private const string RefusalStopReason = "refusal";

    private readonly AiOptions _options = options.Value;

    private AnthropicClient? _client;

    public AiProvider Provider => AiProvider.Claude;

    public bool IsConfigured => _options.Claude.IsConfigured;

    public async Task<AiCompletion> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken)
    {
        AnthropicClient client = _client ??= new AnthropicClient { ApiKey = _options.Claude.ApiKey, HttpClient = httpClient };

        BetaMessage message = await client.Beta.Messages.Create(
            new MessageCreateParams
            {
                Model = _options.Claude.Model,
                MaxTokens = _options.MaxOutputTokens,
                System = prompt.System,
                Messages = [new() { Role = Role.User, Content = prompt.User }],
                Betas = [ServerSideFallbackBeta],
                Fallbacks = new Default(),
            },
            cancellationToken);

        string model = message.Model.Raw();
        if (message.StopReason == RefusalStopReason)
        {
            return new AiCompletion(string.Empty, model, Refused: true);
        }

        string text = string.Concat(message.Content.Select(block => block.TryPickText(out BetaTextBlock? textBlock) ? textBlock.Text : string.Empty));
        return new AiCompletion(text, model, Refused: false);
    }

    public void Dispose() => _client?.Dispose();
}

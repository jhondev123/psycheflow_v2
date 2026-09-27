using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Psycheflow.Api.Features.Ai.Providers;

/// <summary>OpenAI via SDK oficial, Chat Completions.</summary>
public sealed class OpenAiTextGenerator(HttpClient httpClient, IOptions<AiOptions> options) : IAiTextGenerator
{
    private readonly AiOptions _options = options.Value;

    public AiProvider Provider => AiProvider.OpenAi;

    public bool IsConfigured => _options.OpenAi.IsConfigured;

    public async Task<AiCompletion> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken)
    {
        using var transport = new HttpClientPipelineTransport(httpClient);
        var client = new ChatClient(
            _options.OpenAi.Model,
            new ApiKeyCredential(_options.OpenAi.ApiKey!),
            new OpenAIClientOptions { Transport = transport });

        ChatCompletion completion = (await client.CompleteChatAsync(
            [new SystemChatMessage(prompt.System), new UserChatMessage(prompt.User)],
            new ChatCompletionOptions { MaxOutputTokenCount = _options.MaxOutputTokens },
            cancellationToken)).Value;

        if (!string.IsNullOrEmpty(completion.Refusal))
        {
            return new AiCompletion(string.Empty, completion.Model, Refused: true);
        }

        string text = string.Concat(completion.Content.Select(part => part.Text));
        return new AiCompletion(text, completion.Model, Refused: false);
    }
}

using System.Text.Json;
using Microsoft.Extensions.Options;
using Psycheflow.Api.Features.Ai.Providers;

namespace Psycheflow.Api.UnitTests.Features.Ai;

/// <summary>
/// Os três provedores recebem o mesmo <see cref="AiPrompt"/>; aqui se verifica o que cada SDK envia e como a resposta é lida,
/// sem chamar as APIs reais (HttpClient com handler fixo).
/// </summary>
public sealed class TextGeneratorTests
{
    private static readonly AiPrompt Prompt = new("instruções do sistema", "dados e tarefa");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static IOptions<AiOptions> Options(string? apiKey = "test-key") => Microsoft.Extensions.Options.Options.Create(new AiOptions
    {
        MaxOutputTokens = 2000,
        Claude = new AiProviderOptions { ApiKey = apiKey, Model = "claude-opus-5" },
        OpenAi = new AiProviderOptions { ApiKey = apiKey, Model = "gpt-5" },
        Gemini = new AiProviderOptions { ApiKey = apiKey, Model = "gemini-2.5-pro" },
    });

    [Fact]
    public void Generators_AreConfiguredOnlyWithAnApiKey()
    {
        using var http = new HttpClient();
        using var configured = new ClaudeTextGenerator(http, Options());
        using var withoutKey = new ClaudeTextGenerator(http, Options(apiKey: " "));

        configured.IsConfigured.ShouldBeTrue();
        withoutKey.IsConfigured.ShouldBeFalse();
        new OpenAiTextGenerator(http, Options(apiKey: null)).IsConfigured.ShouldBeFalse();
        new GeminiTextGenerator(http, Options(apiKey: "")).IsConfigured.ShouldBeFalse();
    }

    [Fact]
    public async Task Claude_SendsPromptWithServerSideFallback_AndJoinsTheTextBlocks()
    {
        using var handler = new StubHttpHandler("""
            {"id":"msg_01","type":"message","role":"assistant","model":"claude-opus-5",
             "content":[{"type":"text","text":"Parte 1. "},{"type":"text","text":"Parte 2."}],
             "stop_reason":"end_turn","stop_sequence":null,"usage":{"input_tokens":12,"output_tokens":34}}
            """);
        using var http = new HttpClient(handler);

        using var generator = new ClaudeTextGenerator(http, Options());

        AiCompletion completion = await generator.GenerateAsync(Prompt, Ct);
        AiCompletion second = await generator.GenerateAsync(Prompt, Ct);

        second.ShouldBe(completion);
        completion.ShouldBe(new AiCompletion("Parte 1. Parte 2.", "claude-opus-5", Refused: false));
        handler.Request!.RequestUri!.AbsolutePath.ShouldEndWith("/v1/messages");
        handler.Header("x-api-key").ShouldBe("test-key");
        handler.Header("anthropic-beta").ShouldContain(ClaudeTextGenerator.ServerSideFallbackBeta);
        handler.Body.GetProperty("model").GetString().ShouldBe("claude-opus-5");
        handler.Body.GetProperty("max_tokens").GetInt32().ShouldBe(2000);
        handler.Body.GetProperty("system").GetString().ShouldBe("instruções do sistema");
        handler.Body.GetProperty("fallbacks").GetString().ShouldBe("default");
        JsonElement message = handler.Body.GetProperty("messages")[0];
        message.GetProperty("role").GetString().ShouldBe("user");
        message.GetProperty("content").GetString().ShouldBe("dados e tarefa");
    }

    [Fact]
    public async Task Claude_Refusal_IsReportedAsRefused()
    {
        using var handler = new StubHttpHandler("""
            {"id":"msg_02","type":"message","role":"assistant","model":"claude-opus-5","content":[],
             "stop_reason":"refusal","stop_sequence":null,"usage":{"input_tokens":12,"output_tokens":0}}
            """);
        using var http = new HttpClient(handler);

        using var generator = new ClaudeTextGenerator(http, Options());

        AiCompletion completion = await generator.GenerateAsync(Prompt, Ct);

        completion.Refused.ShouldBeTrue();
        completion.Text.ShouldBeEmpty();
    }

    [Fact]
    public async Task OpenAi_SendsSystemAndUserMessages_AndReadsTheContent()
    {
        using var handler = new StubHttpHandler("""
            {"id":"chatcmpl-1","object":"chat.completion","created":1760000000,"model":"gpt-5-2025-08-07",
             "choices":[{"index":0,"message":{"role":"assistant","content":"Sugestão OpenAI","refusal":null},"finish_reason":"stop"}],
             "usage":{"prompt_tokens":10,"completion_tokens":5,"total_tokens":15}}
            """);
        using var http = new HttpClient(handler);

        AiCompletion completion = await new OpenAiTextGenerator(http, Options()).GenerateAsync(Prompt, Ct);

        completion.ShouldBe(new AiCompletion("Sugestão OpenAI", "gpt-5-2025-08-07", Refused: false));
        handler.Request!.RequestUri!.AbsolutePath.ShouldEndWith("/chat/completions");
        handler.Request.Headers.Authorization!.ToString().ShouldBe("Bearer test-key");
        handler.Body.GetProperty("model").GetString().ShouldBe("gpt-5");
        handler.Body.GetProperty("max_completion_tokens").GetInt32().ShouldBe(2000);
        JsonElement messages = handler.Body.GetProperty("messages");
        messages[0].GetProperty("role").GetString().ShouldBe("system");
        messages[1].GetProperty("role").GetString().ShouldBe("user");
        messages[1].GetProperty("content").GetString().ShouldBe("dados e tarefa");
    }

    [Fact]
    public async Task OpenAi_Refusal_IsReportedAsRefused()
    {
        using var handler = new StubHttpHandler("""
            {"id":"chatcmpl-2","object":"chat.completion","created":1760000000,"model":"gpt-5",
             "choices":[{"index":0,"message":{"role":"assistant","content":null,"refusal":"Não posso ajudar."},"finish_reason":"stop"}]}
            """);
        using var http = new HttpClient(handler);

        AiCompletion completion = await new OpenAiTextGenerator(http, Options()).GenerateAsync(Prompt, Ct);

        completion.Refused.ShouldBeTrue();
    }

    [Fact]
    public async Task Gemini_SendsSystemInstructionAndContents_AndReadsTheText()
    {
        using var handler = new StubHttpHandler("""
            {"candidates":[{"content":{"role":"model","parts":[{"text":"Sugestão Gemini"}]},"finishReason":"STOP"}],
             "modelVersion":"gemini-2.5-pro-001"}
            """);
        using var http = new HttpClient(handler);

        AiCompletion completion = await new GeminiTextGenerator(http, Options()).GenerateAsync(Prompt, Ct);

        completion.ShouldBe(new AiCompletion("Sugestão Gemini", "gemini-2.5-pro-001", Refused: false));
        handler.Request!.RequestUri!.AbsolutePath.ShouldEndWith("gemini-2.5-pro:generateContent");
        handler.Header("x-goog-api-key").ShouldBe("test-key");
        handler.Body.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString().ShouldBe("instruções do sistema");
        handler.Body.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString().ShouldBe("dados e tarefa");
        handler.Body.GetProperty("generationConfig").GetProperty("maxOutputTokens").GetInt32().ShouldBe(2000);
    }

    [Fact]
    public async Task Gemini_BlockedPrompt_IsReportedAsRefused()
    {
        using var handler = new StubHttpHandler("""{"promptFeedback":{"blockReason":"SAFETY"},"modelVersion":"gemini-2.5-pro"}""");
        using var http = new HttpClient(handler);

        AiCompletion completion = await new GeminiTextGenerator(http, Options()).GenerateAsync(Prompt, Ct);

        completion.Refused.ShouldBeTrue();
    }
}

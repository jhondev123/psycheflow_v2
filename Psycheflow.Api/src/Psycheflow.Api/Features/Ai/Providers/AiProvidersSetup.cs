namespace Psycheflow.Api.Features.Ai.Providers;

/// <summary>Provedores disponíveis no servidor (os que têm chave configurada).</summary>
public sealed class AiProviderCatalog(IEnumerable<IAiTextGenerator> generators)
{
    private readonly IReadOnlyList<IAiTextGenerator> _configured = [.. generators.Where(g => g.IsConfigured)];

    public IReadOnlyList<AiProvider> Available => [.. _configured.Select(g => g.Provider).Distinct().Order()];

    public bool IsAvailable(AiProvider provider) => Find(provider) is not null;

    public IAiTextGenerator? Find(AiProvider provider) => _configured.FirstOrDefault(g => g.Provider == provider);
}

public static class AiProvidersSetup
{
    public static IServiceCollection AddAiProviders(this IServiceCollection services)
    {
        services.AddOptions<AiOptions>().BindConfiguration(AiOptions.SectionName);

        // O tempo limite de cada sugestão é controlado pelo AiAssistant (CancellationToken), não pelo HttpClient.
        services.AddHttpClient<ClaudeTextGenerator>(DisableHttpTimeout);
        services.AddHttpClient<OpenAiTextGenerator>(DisableHttpTimeout);
        services.AddHttpClient<GeminiTextGenerator>(DisableHttpTimeout);

        services.AddTransient<IAiTextGenerator>(sp => sp.GetRequiredService<ClaudeTextGenerator>());
        services.AddTransient<IAiTextGenerator>(sp => sp.GetRequiredService<OpenAiTextGenerator>());
        services.AddTransient<IAiTextGenerator>(sp => sp.GetRequiredService<GeminiTextGenerator>());
        services.AddScoped<AiProviderCatalog>();

        return services;
    }

    private static void DisableHttpTimeout(HttpClient client) => client.Timeout = Timeout.InfiniteTimeSpan;
}

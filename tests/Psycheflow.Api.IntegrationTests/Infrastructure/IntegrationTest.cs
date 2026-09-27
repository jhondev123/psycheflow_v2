using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.IntegrationTests.Infrastructure;

/// <summary>Base dos testes de integração: banco limpo e relógio reiniciado antes de cada teste.</summary>
public abstract class IntegrationTest(ApiFactory factory) : IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected ApiFactory Factory { get; } = factory;

    /// <summary>Cliente sem autenticação.</summary>
    protected HttpClient Client { get; } = factory.CreateClient();

    public virtual async ValueTask InitializeAsync() => await Factory.ResetAsync();

    public virtual ValueTask DisposeAsync()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected HttpClient CreateClient(string? accessToken = null)
    {
        HttpClient client = Factory.CreateClient();
        if (accessToken is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client;
    }

    /// <summary>Acesso direto ao banco para montar cenários ou conferir o que foi gravado (sem filtro de empresa).</summary>
    protected async Task<T> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(db);
    }

    protected async Task ExecuteDbAsync(Func<AppDbContext, Task> action)
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        T? body = await response.Content.ReadFromJsonAsync<T>(Json);
        return body ?? throw new InvalidOperationException("Resposta sem corpo.");
    }

    protected static Task<ValidationProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        return ReadAsync<ValidationProblemDetails>(response);
    }
}

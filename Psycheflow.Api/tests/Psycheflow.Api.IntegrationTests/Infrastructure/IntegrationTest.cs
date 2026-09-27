using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Auth;
using Psycheflow.Api.Features.Auth.Me;
using Psycheflow.Api.Features.Users.CreateUser;

namespace Psycheflow.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Base dos testes de integração: banco limpo e relógio reiniciado antes de cada teste,
/// e helpers para criar contas pela própria API (como um cliente real faria).
/// </summary>
public abstract class IntegrationTest(ApiFactory factory) : IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly List<HttpClient> _clients = [];

    protected ApiFactory Factory { get; } = factory;

    /// <summary>Cliente sem autenticação.</summary>
    protected HttpClient Client => CreateClient();

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync() => await Factory.ResetAsync();

    public virtual ValueTask DisposeAsync()
    {
        foreach (HttpClient client in _clients)
        {
            client.Dispose();
        }

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

        _clients.Add(client);
        return client;
    }

    protected HttpClient CreateClient(TestAccount account) => CreateClient(account.AccessToken);

    // ---------- Contas ----------

    /// <summary>Registra uma nova empresa. O usuário responsável é Admin + Psychologist.</summary>
    protected async Task<TestAccount> RegisterAccountAsync(string? email = null)
    {
        string accountEmail = email ?? TestAccounts.UniqueEmail();
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/auth/register", TestAccounts.RegisterPayload(accountEmail), Ct);
        response.EnsureSuccessStatusCode();
        AuthResponse auth = await ReadAsync<AuthResponse>(response);

        return await LoadAccountAsync(auth.AccessToken, accountEmail, TestAccounts.DefaultPassword);
    }

    /// <summary>Cria um usuário na empresa do Admin e já troca a senha temporária.</summary>
    protected async Task<TestAccount> CreateStaffAsync(TestAccount admin, string role)
    {
        string email = TestAccounts.UniqueEmail();
        HttpResponseMessage created = await CreateClient(admin).PostAsJsonAsync("/api/v1/users", new
        {
            fullName = TestAccounts.FullName(),
            email,
            role,
            licenseNumber = role == Roles.Psychologist ? TestAccounts.LicenseNumber() : null,
        }, Ct);
        created.EnsureSuccessStatusCode();
        CreateUserResponse user = await ReadAsync<CreateUserResponse>(created);

        AuthResponse firstLogin = await LoginAsync(email, user.TemporaryPassword);
        HttpResponseMessage changed = await CreateClient(firstLogin.AccessToken).PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = user.TemporaryPassword,
            newPassword = TestAccounts.DefaultPassword,
        }, Ct);
        changed.EnsureSuccessStatusCode();
        AuthResponse auth = await ReadAsync<AuthResponse>(changed);

        return await LoadAccountAsync(auth.AccessToken, email, TestAccounts.DefaultPassword);
    }

    /// <summary>
    /// Avança o relógio da API para um horário local de São Paulo. Tokens emitidos antes podem expirar
    /// (validade de 2h): use <see cref="RefreshAsync"/> para renovar as contas depois.
    /// </summary>
    protected void TravelTo(DateOnly date, TimeOnly time) => Factory.Clock.SetLocal(date, time);

    /// <summary>Faz login de novo e devolve a conta com um token válido no horário atual do relógio.</summary>
    protected async Task<TestAccount> RefreshAsync(TestAccount account) =>
        account with { AccessToken = (await LoginAsync(account.Email, account.Password)).AccessToken };

    protected async Task<AuthResponse> LoginAsync(string email, string password)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }, Ct);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<AuthResponse>(response);
    }

    private async Task<TestAccount> LoadAccountAsync(string accessToken, string email, string password)
    {
        MeResponse me = (await CreateClient(accessToken).GetFromJsonAsync<MeResponse>("/api/v1/auth/me", Json, Ct))!;
        return new TestAccount(accessToken, me.Id, me.CompanyId, me.PsychologistId, email, password);
    }

    // ---------- Banco ----------

    /// <summary>Acesso direto ao banco para montar cenários ou conferir o que foi gravado (sem usuário logado).</summary>
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

    // ---------- HTTP ----------

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        T? body = await response.Content.ReadFromJsonAsync<T>(Json, Ct);
        return body ?? throw new InvalidOperationException("Resposta sem corpo.");
    }

    protected static async Task<ValidationProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        return await ReadAsync<ValidationProblemDetails>(response);
    }
}

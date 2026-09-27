using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Psycheflow.Api.Common.RateLimiting;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Common;

/// <summary>
/// DT-23. A suíte usa limites altos (ApiFactory); aqui cada teste sobe uma instância com limite baixo,
/// para que os contadores não interfiram nos outros testes.
/// </summary>
public sealed class RateLimitingTests(ApiFactory factory) : IntegrationTest(factory)
{
    private WebApplicationFactory<Program> WithLimit(string policy, int permitLimit) =>
        Factory.WithWebHostBuilder(builder => builder.UseSetting($"RateLimiting:{policy}:PermitLimit", permitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task Login_TooManyAttemptsFromTheSameIp_Returns429WithRetryAfter()
    {
        await using WebApplicationFactory<Program> limited = WithLimit("Auth", permitLimit: 2);
        using HttpClient client = limited.CreateClient();
        var credentials = new { email = "ninguem@teste.psycheflow.dev", password = "Errada@123" };

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/v1/auth/login", credentials, Ct);
        HttpResponseMessage second = await client.PostAsJsonAsync("/api/v1/auth/login", credentials, Ct);
        HttpResponseMessage third = await client.PostAsJsonAsync("/api/v1/auth/login", credentials, Ct);

        first.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        second.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        third.Headers.RetryAfter.ShouldNotBeNull();
        (await ReadAsync<ProblemDetails>(third)).Extensions["code"]!.ToString().ShouldBe(RateLimitingSetup.ExceededCode);
    }

    [Fact]
    public async Task AiSuggestions_AreLimitedPerUser()
    {
        TestAccount first = await RegisterAccountAsync();
        TestAccount second = await RegisterAccountAsync();
        await using WebApplicationFactory<Program> limited = WithLimit("Ai", permitLimit: 1);

        HttpClient Client(TestAccount account)
        {
            HttpClient client = limited.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
            return client;
        }

        using HttpClient firstClient = Client(first);
        using HttpClient secondClient = Client(second);
        var body = new { patientId = Guid.CreateVersion7() };

        HttpResponseMessage allowed = await firstClient.PostAsJsonAsync("/api/v1/ai/suggestions/next-steps", body, Ct);
        HttpResponseMessage limitedResponse = await firstClient.PostAsJsonAsync("/api/v1/ai/suggestions/next-steps", body, Ct);
        HttpResponseMessage otherUser = await secondClient.PostAsJsonAsync("/api/v1/ai/suggestions/next-steps", body, Ct);

        allowed.StatusCode.ShouldBe(HttpStatusCode.Forbidden); // IA desabilitada: passou pelo limitador
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        otherUser.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}

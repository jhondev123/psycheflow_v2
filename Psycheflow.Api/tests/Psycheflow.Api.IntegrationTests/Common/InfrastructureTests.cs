using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Common;

public sealed class InfrastructureTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Health_WithDatabaseUp_ReturnsHealthy()
    {
        HttpResponseMessage response = await Client.GetAsync("/health", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Migrations_CreateEssentialRoles()
    {
        List<string?> roles = await QueryDbAsync(db => db.Roles.Select(r => r.Name).ToListAsync());

        roles.ShouldBe(Roles.All, ignoreOrder: true);
    }

    [Fact]
    public async Task ProtectedRoute_WithoutToken_Returns401ProblemDetails()
    {
        HttpResponseMessage response = await Client.GetAsync("/api/v1/rota-inexistente", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Cors_AllowedOrigin_CanReadTheDownloadFileName()
    {
        const string front = "http://localhost:5173";
        await using WebApplicationFactory<Program> app = Factory.WithWebHostBuilder(builder => builder.UseSetting("Cors:AllowedOrigins:0", front));
        using HttpClient client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", front);

        HttpResponseMessage response = await client.SendAsync(request, Ct);

        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe([front]);
        response.Headers.GetValues("Access-Control-Expose-Headers").ShouldContain(value => value.Contains("Content-Disposition"));
    }
}

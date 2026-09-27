using System.Net;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Common;

public sealed class InfrastructureTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Health_WithDatabaseUp_ReturnsHealthy()
    {
        HttpResponseMessage response = await Client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("Healthy");
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
        HttpResponseMessage response = await Client.GetAsync("/api/v1/rota-inexistente", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}

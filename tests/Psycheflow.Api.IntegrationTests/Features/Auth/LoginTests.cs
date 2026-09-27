using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Psycheflow.Api.Features.Auth;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Auth;

public sealed class LoginTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsWorkingToken()
    {
        TestAccount account = await RegisterAccountAsync();

        AuthResponse auth = await LoginAsync(account.Email, account.Password);

        HttpResponseMessage me = await CreateClient(auth.AccessToken).GetAsync("/api/v1/auth/me", Ct);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email = account.Email.ToUpperInvariant(), password = account.Password }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_InvalidCredentials_Returns401WithGenericMessage(bool existingEmail)
    {
        TestAccount account = await RegisterAccountAsync();
        string email = existingEmail ? account.Email : TestAccounts.UniqueEmail();

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Errada@123" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ProblemDetails problem = await ReadProblemAsync(response);
        problem.Detail.ShouldBe("E-mail ou senha inválidos.");
    }

    [Fact]
    public async Task Login_AfterFiveWrongAttempts_LocksAccount()
    {
        TestAccount account = await RegisterAccountAsync();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await Client.PostAsJsonAsync("/api/v1/auth/login", new { email = account.Email, password = "Errada@123" }, Ct);
        }

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email = account.Email, password = account.Password }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Locked);
    }

    [Fact]
    public async Task Token_AfterExpiration_IsRejected()
    {
        TestAccount account = await RegisterAccountAsync();

        Factory.Clock.Advance(TimeSpan.FromMinutes(121));

        HttpResponseMessage response = await CreateClient(account).GetAsync("/api/v1/auth/me", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

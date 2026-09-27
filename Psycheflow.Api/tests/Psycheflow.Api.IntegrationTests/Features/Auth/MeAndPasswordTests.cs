using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Auth;
using Psycheflow.Api.Features.Auth.Me;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Auth;

public sealed class MeAndPasswordTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Me_ReturnsProfileRolesAndPsychologistId()
    {
        TestAccount account = await RegisterAccountAsync();

        MeResponse me = (await CreateClient(account).GetFromJsonAsync<MeResponse>("/api/v1/auth/me", Json, Ct))!;

        me.Email.ShouldBe(account.Email);
        me.Roles.ShouldBe([Roles.Admin, Roles.Psychologist], ignoreOrder: true);
        me.PsychologistId.ShouldNotBeNull();
        me.CompanyName.ShouldStartWith("Clínica");
        me.MustChangePassword.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns422()
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Errada@123",
            newPassword = "NovaSenha@456",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails problem = await ReadProblemAsync(response);
        problem.Errors.ShouldContainKey("currentPassword");
    }

    [Fact]
    public async Task ChangePassword_Valid_OnlyNewPasswordWorks()
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = account.Password,
            newPassword = "NovaSenha@456",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync<AuthResponse>(response)).AccessToken.ShouldNotBeNullOrWhiteSpace();

        HttpResponseMessage oldLogin = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email = account.Email, password = account.Password }, Ct);
        oldLogin.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await LoginAsync(account.Email, "NovaSenha@456");
    }
}

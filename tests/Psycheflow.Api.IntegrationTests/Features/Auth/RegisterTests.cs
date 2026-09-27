using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Auth;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Auth;

public sealed class RegisterTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Register_ValidData_CreatesCompanyOwnerAndPsychologistProfile()
    {
        string email = TestAccounts.UniqueEmail();

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/auth/register", TestAccounts.RegisterPayload(email), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        AuthResponse auth = await ReadAsync<AuthResponse>(response);
        auth.AccessToken.ShouldNotBeNullOrWhiteSpace();
        auth.MustChangePassword.ShouldBeFalse();
        auth.ExpiresAt.ShouldBe(ApiFactory.DefaultNow.AddMinutes(120));

        var saved = await QueryDbAsync(async db =>
        {
            var user = await db.Users.Include(u => u.Company).SingleAsync(u => u.Email == email);
            List<string?> roles = await db.UserRoles.Where(ur => ur.UserId == user.Id)
                .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name).ToListAsync();
            Psychologist psychologist = await db.Psychologists.IgnoreQueryFilters().SingleAsync(p => p.UserId == user.Id);
            return new { user, roles, psychologist };
        });

        saved.user.Company!.Settings.SessionDurationMinutes.ShouldBe(50);
        saved.user.CreatedAt.ShouldBe(ApiFactory.DefaultNow);
        saved.roles.ShouldBe([Roles.Admin, Roles.Psychologist], ignoreOrder: true);
        saved.psychologist.CompanyId.ShouldBe(saved.user.CompanyId);
        saved.psychologist.Approach.ShouldBe(ApproachType.CognitiveBehavioral);
        saved.psychologist.Phone!.Value.ShouldBe("45999991234");
    }

    [Fact]
    public async Task Register_EmailAlreadyInUse_Returns409()
    {
        string email = TestAccounts.UniqueEmail();
        await RegisterAccountAsync(email);

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/auth/register", TestAccounts.RegisterPayload(email.ToUpperInvariant()), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        int companies = await QueryDbAsync(db => db.Companies.CountAsync());
        companies.ShouldBe(1);
    }

    [Fact]
    public async Task Register_InvalidFields_Returns422WithErrorsPerField()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "",
            fullName = "Ana",
            email = "nao-e-email",
            password = "Senha@123",
            licenseNumber = "12345",
            phone = "123",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails problem = await ReadProblemAsync(response);
        problem.Errors.Keys.ShouldBe(["companyName", "email", "licenseNumber", "phone"], ignoreOrder: true);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns422OnPasswordAndCreatesNothing()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/v1/auth/register", TestAccounts.RegisterPayload(password: "senhafraca"), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails problem = await ReadProblemAsync(response);
        problem.Errors.ShouldContainKey("password");
        (await QueryDbAsync(db => db.Companies.CountAsync())).ShouldBe(0);
        (await QueryDbAsync(db => db.Users.CountAsync())).ShouldBe(0);
    }
}

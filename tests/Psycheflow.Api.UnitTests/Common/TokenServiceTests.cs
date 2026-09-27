using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Psycheflow.Api.Common.Auth;

namespace Psycheflow.Api.UnitTests.Common;

public sealed class TokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private static TokenService CreateService() => new(
        Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            Key = "unit-test-signing-key-0123456789abcdefgh",
            ExpirationMinutes = 60,
        }),
        new FakeTimeProvider(Now));

    [Fact]
    public void Create_IncludesIdentityCompanyAndRoleClaims()
    {
        var subject = new TokenSubject(
            UserId: Guid.CreateVersion7(),
            Email: "ana@psycheflow.dev",
            FullName: "Ana Souza",
            CompanyId: Guid.CreateVersion7(),
            PsychologistId: Guid.CreateVersion7(),
            MustChangePassword: false,
            Roles: [Roles.Admin, Roles.Psychologist]);

        AccessToken token = CreateService().Create(subject);

        JsonWebToken jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.Token);
        jwt.Subject.ShouldBe(subject.UserId.ToString());
        jwt.GetClaim(AuthClaims.CompanyId).Value.ShouldBe(subject.CompanyId.ToString());
        jwt.GetClaim(AuthClaims.PsychologistId).Value.ShouldBe(subject.PsychologistId.ToString());
        jwt.Claims.Where(c => c.Type == AuthClaims.Role).Select(c => c.Value).ShouldBe([Roles.Admin, Roles.Psychologist], ignoreOrder: true);
        jwt.TryGetClaim(AuthClaims.MustChangePassword, out _).ShouldBeFalse();
    }

    [Fact]
    public void Create_ExpiresAfterConfiguredMinutes()
    {
        AccessToken token = CreateService().Create(Subject(mustChangePassword: false));

        token.ExpiresAt.ShouldBe(Now.AddMinutes(60));
        new JsonWebTokenHandler().ReadJsonWebToken(token.Token).ValidTo.ShouldBe(Now.AddMinutes(60).UtcDateTime);
    }

    [Fact]
    public void Create_WithPendingPasswordChange_AddsRestrictionClaim()
    {
        AccessToken token = CreateService().Create(Subject(mustChangePassword: true));

        new JsonWebTokenHandler().ReadJsonWebToken(token.Token)
            .GetClaim(AuthClaims.MustChangePassword).Value.ShouldBe("true");
    }

    private static TokenSubject Subject(bool mustChangePassword) => new(
        Guid.CreateVersion7(), "user@psycheflow.dev", "Usuário", Guid.CreateVersion7(), null, mustChangePassword, [Roles.Manager]);
}

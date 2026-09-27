using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Psycheflow.Api.Common.Auth;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record TokenSubject(
    Guid UserId,
    string Email,
    string FullName,
    Guid CompanyId,
    Guid? PsychologistId,
    bool MustChangePassword,
    IReadOnlyCollection<string> Roles);

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Create(TokenSubject subject)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = now.AddMinutes(_options.ExpirationMinutes);

        List<Claim> claims =
        [
            new(AuthClaims.UserId, subject.UserId.ToString()),
            new(AuthClaims.Email, subject.Email),
            new(AuthClaims.Name, subject.FullName),
            new(AuthClaims.CompanyId, subject.CompanyId.ToString()),
            .. subject.Roles.Select(role => new Claim(AuthClaims.Role, role)),
        ];

        if (subject.PsychologistId is { } psychologistId)
        {
            claims.Add(new Claim(AuthClaims.PsychologistId, psychologistId.ToString()));
        }

        if (subject.MustChangePassword)
        {
            claims.Add(new Claim(AuthClaims.MustChangePassword, "true"));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(CreateSigningKey(_options.Key), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }

    public static SymmetricSecurityKey CreateSigningKey(string key) => new(Encoding.UTF8.GetBytes(key));
}

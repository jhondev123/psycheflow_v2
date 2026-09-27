using System.ComponentModel.DataAnnotations;

namespace Psycheflow.Api.Common.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>Chave HMAC-SHA256. A chave de produção nunca é versionada (variável de ambiente <c>Jwt__Key</c>).</summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:Key precisa ter pelo menos 32 caracteres.")]
    public string Key { get; init; } = string.Empty;

    [Range(5, 1440)]
    public int ExpirationMinutes { get; init; } = 120;
}

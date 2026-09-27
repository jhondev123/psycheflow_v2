namespace Psycheflow.Api.Features.Auth;

/// <param name="AccessToken">JWT para o cabeçalho <c>Authorization: Bearer</c>.</param>
/// <param name="ExpiresAt">Momento (UTC) em que o token expira.</param>
/// <param name="MustChangePassword">Quando verdadeiro, só <c>/auth/me</c> e <c>/auth/change-password</c> estão liberados.</param>
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, bool MustChangePassword);

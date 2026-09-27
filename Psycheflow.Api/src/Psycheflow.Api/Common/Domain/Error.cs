namespace Psycheflow.Api.Common.Domain;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
    Locked,

    /// <summary>Dependência externa (ex.: provedor de IA) indisponível ou com falha.</summary>
    Unavailable,
}

/// <summary>
/// Falha esperada de negócio. <paramref name="Code"/> é estável (para o front tratar),
/// <paramref name="Message"/> é o texto em pt-BR e <paramref name="Field"/> (camelCase) indica o campo do request, quando houver.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type, string? Field = null)
{
    public static Error Validation(string code, string message, string? field = null) =>
        new(code, message, ErrorType.Validation, field);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Locked(string code, string message) => new(code, message, ErrorType.Locked);

    public static Error Unavailable(string code, string message) => new(code, message, ErrorType.Unavailable);
}

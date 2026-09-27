using System.Diagnostics.CodeAnalysis;

namespace Psycheflow.Api.Common.Domain;

/// <summary>
/// Resultado de uma operação que pode falhar por regra de negócio.
/// Exceções ficam reservadas para erros inesperados (viram HTTP 500).
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(null) => _value = value;

    private Result(Error error)
        : base(error)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Não é possível ler o valor de um resultado com falha ({Error.Code}).");

    /// <summary>Sucesso explícito — necessário quando <typeparamref name="T"/> é uma interface (sem conversão implícita).</summary>
    public static Result<T> Success(T value) => new(value);

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}

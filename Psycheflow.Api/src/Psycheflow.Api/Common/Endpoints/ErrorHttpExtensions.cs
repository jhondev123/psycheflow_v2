using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Common.Endpoints;

/// <summary>Converte falhas de negócio (<see cref="Error"/>) em respostas ProblemDetails (RFC 9457).</summary>
public static class ErrorHttpExtensions
{
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Locked => StatusCodes.Status423Locked,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static ProblemHttpResult ToProblem(this Error error)
    {
        int status = error.Type.ToStatusCode();

        ProblemDetails problem = error.Field is null
            ? new ProblemDetails()
            : new HttpValidationProblemDetails(new Dictionary<string, string[]> { [error.Field] = [error.Message] });

        problem.Status = status;
        problem.Title = TitleFor(error.Type);
        problem.Detail = error.Message;
        problem.Extensions["code"] = error.Code;

        return TypedResults.Problem(problem);
    }

    public static ProblemHttpResult ToProblem(this Result result) =>
        result.IsFailure
            ? result.Error.ToProblem()
            : throw new InvalidOperationException("Um resultado de sucesso não pode ser convertido em ProblemDetails.");

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Dados inválidos.",
        ErrorType.NotFound => "Recurso não encontrado.",
        ErrorType.Conflict => "Conflito com o estado atual.",
        ErrorType.Forbidden => "Acesso negado.",
        ErrorType.Unauthorized => "Não autenticado.",
        ErrorType.Locked => "Recurso bloqueado.",
        ErrorType.Unavailable => "Serviço indisponível.",
        _ => "Erro inesperado.",
    };
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Psycheflow.Api.Common.Errors;

/// <summary>
/// Último recurso para exceções não tratadas: registra o erro e devolve ProblemDetails sem vazar detalhes internos.
/// Erros de leitura do request (JSON malformado, parâmetro inválido) viram 400.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is BadHttpRequestException badRequest)
        {
            problem = new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Requisição inválida.",
                Detail = "O corpo ou os parâmetros da requisição estão em formato inválido.",
            };
        }
        else
        {
            logger.LogError(exception, "Erro não tratado em {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno.",
                Detail = "Ocorreu um erro inesperado. Tente novamente mais tarde.",
            };
        }

        httpContext.Response.StatusCode = problem.Status.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }
}

using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;

namespace Psycheflow.Api.Common.Endpoints;

/// <summary>
/// Executa o <see cref="IValidator{T}"/> do request antes do handler. Falhas viram 422 com os erros por campo (camelCase).
/// O validator é resolvido do escopo da requisição para respeitar o tempo de vida registrado.
/// </summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        TRequest? request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        IValidator<TRequest>? validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();

        if (request is null || validator is null)
        {
            return await next(context);
        }

        ValidationResult result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return await next(context);
        }

        return ValidationProblems.From(result.Errors);
    }
}

public static class ValidationProblems
{
    public static IResult From(IEnumerable<ValidationFailure> failures)
    {
        Dictionary<string, string[]> errors = failures
            .GroupBy(f => ToCamelCasePath(f.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        return TypedResults.Problem(new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Dados inválidos.",
            Detail = "Um ou mais campos não passaram na validação.",
            Extensions = { ["code"] = "validation_failed" },
        });
    }

    /// <summary>"Address.ZipCode" → "address.zipCode"; "Hours[0].StartTime" → "hours[0].startTime".</summary>
    public static string ToCamelCasePath(string propertyPath) =>
        string.Join('.', propertyPath.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithRequestValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
}

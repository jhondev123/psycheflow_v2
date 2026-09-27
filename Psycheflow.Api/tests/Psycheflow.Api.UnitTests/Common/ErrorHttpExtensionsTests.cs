using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.UnitTests.Common;

public sealed class ErrorHttpExtensionsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status422UnprocessableEntity)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Locked, StatusCodes.Status423Locked)]
    [InlineData(ErrorType.Unavailable, StatusCodes.Status503ServiceUnavailable)]
    public void ToStatusCode_MapsEachErrorType(ErrorType type, int expectedStatus) =>
        type.ToStatusCode().ShouldBe(expectedStatus);

    [Fact]
    public void ToProblem_WithoutField_ReturnsProblemWithCodeAndDetail()
    {
        Error error = Error.NotFound("patient.not_found", "Paciente não encontrado.");

        ProblemHttpResult result = error.ToProblem();

        result.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        result.ProblemDetails.Detail.ShouldBe("Paciente não encontrado.");
        result.ProblemDetails.Extensions["code"].ShouldBe("patient.not_found");
        result.ProblemDetails.ShouldNotBeOfType<HttpValidationProblemDetails>();
    }

    [Fact]
    public void ToProblem_WithField_ReturnsValidationProblemWithFieldError()
    {
        Error error = Error.Validation("patient.invalid_cpf", "CPF inválido.", "cpf");

        ProblemHttpResult result = error.ToProblem();

        result.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        HttpValidationProblemDetails problem = result.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();
        problem.Errors["cpf"].ShouldBe(["CPF inválido."]);
    }
}

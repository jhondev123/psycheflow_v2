using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.UnitTests.Common;

public sealed class ValidationProblemsTests
{
    [Theory]
    [InlineData("Name", "name")]
    [InlineData("Address.ZipCode", "address.zipCode")]
    [InlineData("Hours[0].StartTime", "hours[0].startTime")]
    public void ToCamelCasePath_ConvertsEachSegment(string input, string expected) =>
        ValidationProblems.ToCamelCasePath(input).ShouldBe(expected);

    [Fact]
    public void From_GroupsFailuresByFieldWith422()
    {
        ValidationFailure[] failures =
        [
            new("Name", "Informe o nome."),
            new("Cpf", "Informe o CPF."),
            new("Cpf", "CPF inválido."),
        ];

        ProblemHttpResult result = ValidationProblems.From(failures).ShouldBeOfType<ProblemHttpResult>();

        result.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        HttpValidationProblemDetails problem = result.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();
        problem.Errors["name"].ShouldBe(["Informe o nome."]);
        problem.Errors["cpf"].ShouldBe(["Informe o CPF.", "CPF inválido."]);
    }
}

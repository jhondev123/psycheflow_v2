using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Documents;

public static class PdfResults
{
    public static Results<FileContentHttpResult, ProblemHttpResult> ToFileResult(this Result<PdfFile> result) =>
        result.IsSuccess
            ? TypedResults.File(result.Value.Content, PdfFile.ContentType, result.Value.FileName)
            : result.Error.ToProblem();

    public static RouteHandlerBuilder ProducesPdf(this RouteHandlerBuilder builder) =>
        builder.Produces(StatusCodes.Status200OK, contentType: PdfFile.ContentType);
}

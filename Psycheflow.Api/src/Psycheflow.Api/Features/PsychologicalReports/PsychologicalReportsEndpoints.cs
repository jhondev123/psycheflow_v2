using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Features.Documents;
using Psycheflow.Api.Features.PsychologicalReports.CreateReport;
using Psycheflow.Api.Features.PsychologicalReports.ReportActions;
using Psycheflow.Api.Features.PsychologicalReports.UpdateReport;

namespace Psycheflow.Api.Features.PsychologicalReports;

/// <summary>Laudos e relatórios psicológicos. As ações simples (ler, listar, finalizar, excluir, PDF) ficam neste arquivo.</summary>
public static class PsychologicalReportsEndpoints
{
    public static IEndpointRouteBuilder MapPsychologicalReportsEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/psychological-reports").WithTags("Psychological reports");

        CreateReportEndpoint.Map(group);
        UpdateReportEndpoint.Map(group);

        group.MapGet("/", async ([AsParameters] ListReportsQuery query, ListReportsHandler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.Handle(query, ct)))
            .WithName("ListPsychologicalReports")
            .WithSummary("Laudos/relatórios do psicólogo logado (opcionalmente de um paciente).");

        group.MapGet("/{id:guid}", async Task<Results<Ok<PsychologicalReportResponse>, ProblemHttpResult>> (
                Guid id, GetReportHandler handler, CancellationToken ct) => ToOk(await handler.Handle(id, ct)))
            .WithName("GetPsychologicalReport")
            .WithSummary("Detalhe do laudo/relatório (somente o autor).");

        group.MapPost("/{id:guid}/finalize", async Task<Results<Ok<PsychologicalReportResponse>, ProblemHttpResult>> (
                Guid id, FinalizeReportHandler handler, CancellationToken ct) => ToOk(await handler.Handle(id, ct)))
            .WithName("FinalizePsychologicalReport")
            .WithSummary("Finaliza o documento (todas as seções obrigatórias); depois disso não pode ser editado.");

        group.MapDelete("/{id:guid}", async Task<Results<NoContent, ProblemHttpResult>> (
                Guid id, DeleteReportHandler handler, CancellationToken ct) =>
                await handler.Handle(id, ct) is { IsFailure: true } failure ? failure.Error.ToProblem() : TypedResults.NoContent())
            .WithName("DeletePsychologicalReport")
            .WithSummary("Exclui um rascunho.");

        group.MapGet("/{id:guid}/pdf", async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                Guid id, ReportPdfHandler handler, CancellationToken ct) => (await handler.Handle(id, ct)).ToFileResult())
            .WithName("GetPsychologicalReportPdf")
            .WithSummary("PDF do laudo/relatório (rascunhos saem com marca d'água).")
            .ProducesPdf();

        return api;
    }

    private static Results<Ok<PsychologicalReportResponse>, ProblemHttpResult> ToOk(Result<PsychologicalReportResponse> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
}

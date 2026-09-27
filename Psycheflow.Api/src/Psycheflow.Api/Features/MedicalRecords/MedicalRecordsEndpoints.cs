using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Features.MedicalRecords.Attachments;
using Psycheflow.Api.Features.MedicalRecords.RecordCrud;

namespace Psycheflow.Api.Features.MedicalRecords;

/// <summary>Prontuários (RF019/RF020). Os handlers ficam em RecordCrud/ e Attachments/; aqui só o mapeamento HTTP.</summary>
public static class MedicalRecordsEndpoints
{
    public static IEndpointRouteBuilder MapMedicalRecordsEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/medical-records").WithTags("Medical records");

        group.MapPost("/", CreateAsync)
            .WithRequestValidation<CreateMedicalRecordRequest>()
            .WithName("CreateMedicalRecord")
            .WithSummary("Novo registro no prontuário do paciente (somente psicólogos; só o autor tem acesso).");

        group.MapGet("/", ListAsync)
            .WithName("ListMedicalRecords")
            .WithSummary("Busca nos prontuários do psicólogo logado por paciente, período e palavra-chave.");

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetMedicalRecord")
            .WithSummary("Registro do prontuário com a lista de anexos.");

        group.MapGet("/{id:guid}/access-log", AccessLogAsync)
            .WithName("GetMedicalRecordAccessLog")
            .WithSummary("Histórico de acessos ao registro: leituras, downloads e tentativas negadas (somente o autor).");

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithRequestValidation<UpdateMedicalRecordRequest>()
            .WithName("UpdateMedicalRecord")
            .WithSummary("Edita título e conteúdo do registro.");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteMedicalRecord")
            .WithSummary("Exclui (logicamente) o registro.");

        // API autenticada por Bearer (sem cookie): proteção antiforgery não se aplica ao upload.
        group.MapPost("/{id:guid}/attachments", UploadAsync)
            .DisableAntiforgery()
            .WithName("UploadMedicalRecordAttachment")
            .WithSummary("Anexa PDF, JPG ou PNG (até 10 MB) ao registro (multipart/form-data, campo \"file\").");

        group.MapGet("/{id:guid}/attachments/{attachmentId:guid}", DownloadAsync)
            .WithName("DownloadMedicalRecordAttachment")
            .WithSummary("Download do anexo.");

        group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", DeleteAttachmentAsync)
            .WithName("DeleteMedicalRecordAttachment")
            .WithSummary("Remove o anexo do registro.");

        return api;
    }

    private static async Task<Results<Created<MedicalRecordResponse>, ProblemHttpResult>> CreateAsync(
        CreateMedicalRecordRequest request, CreateMedicalRecordHandler handler, CancellationToken cancellationToken)
    {
        Result<MedicalRecordResponse> result = await handler.Handle(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/medical-records/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<Ok<PagedResponse<MedicalRecordListItem>>> ListAsync(
        [AsParameters] ListMedicalRecordsQuery query, ListMedicalRecordsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(query, cancellationToken));

    private static async Task<Results<Ok<MedicalRecordResponse>, ProblemHttpResult>> GetAsync(
        Guid id, GetMedicalRecordHandler handler, CancellationToken cancellationToken) =>
        ToOk(await handler.Handle(id, cancellationToken));

    private static async Task<Results<Ok<IReadOnlyList<MedicalRecordAccessEntry>>, ProblemHttpResult>> AccessLogAsync(
        Guid id, GetAccessLogHandler handler, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<MedicalRecordAccessEntry>> result = await handler.Handle(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<MedicalRecordResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id, UpdateMedicalRecordRequest request, UpdateMedicalRecordHandler handler, CancellationToken cancellationToken) =>
        ToOk(await handler.Handle(id, request, cancellationToken));

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id, DeleteMedicalRecordHandler handler, CancellationToken cancellationToken) =>
        ToNoContent(await handler.Handle(id, cancellationToken));

    private static async Task<Results<Created<AttachmentResponse>, ProblemHttpResult>> UploadAsync(
        Guid id, IFormFile? file, UploadAttachmentHandler handler, CancellationToken cancellationToken)
    {
        Result<AttachmentResponse> result = await handler.Handle(id, file, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/medical-records/{id}/attachments/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> DownloadAsync(
        Guid id, Guid attachmentId, DownloadAttachmentHandler handler, CancellationToken cancellationToken)
    {
        Result<AttachmentDownload> result = await handler.Handle(id, attachmentId, cancellationToken);
        return result.IsSuccess
            ? TypedResults.File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : result.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAttachmentAsync(
        Guid id, Guid attachmentId, DeleteAttachmentHandler handler, CancellationToken cancellationToken) =>
        ToNoContent(await handler.Handle(id, attachmentId, cancellationToken));

    private static Results<Ok<MedicalRecordResponse>, ProblemHttpResult> ToOk(Result<MedicalRecordResponse> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();

    private static Results<NoContent, ProblemHttpResult> ToNoContent(Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
}

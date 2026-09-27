using Psycheflow.Api.Features.Documents.Attendance;
using Psycheflow.Api.Features.Documents.FeedbackReport;
using Psycheflow.Api.Features.Documents.Receipt;
using Psycheflow.Api.Features.Documents.SessionsReport;

namespace Psycheflow.Api.Features.Documents;

public static class DocumentsEndpoints
{
    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/documents").WithTags("Documents");

        GetReceiptEndpoint.Map(group);
        GetAttendanceEndpoint.Map(group);
        SessionsReportEndpoint.Map(group);
        FeedbackReportEndpoint.Map(group);

        return api;
    }
}

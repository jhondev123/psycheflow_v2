using Microsoft.AspNetCore.Http.HttpResults;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;

namespace Psycheflow.Api.Features.Dashboard.GetDashboard;

public static class GetDashboardEndpoint
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/dashboard", HandleAsync)
            .WithTags("Dashboard")
            .WithName("GetDashboard")
            .WithSummary("Painel inicial: números do dia e da semana, agenda de hoje, próximos atendimentos e financeiro.");

        return api;
    }

    private static async Task<Results<Ok<DashboardResponse>, ProblemHttpResult>> HandleAsync(
        [AsParameters] DashboardQuery query, GetDashboardHandler handler, CancellationToken cancellationToken)
    {
        Result<DashboardResponse> result = await handler.Handle(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

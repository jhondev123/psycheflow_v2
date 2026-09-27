using Psycheflow.Api.Features.Scheduling.CreateBlock;
using Psycheflow.Api.Features.Scheduling.DeleteBlock;
using Psycheflow.Api.Features.Scheduling.GetAgenda;

namespace Psycheflow.Api.Features.Scheduling;

public static class SchedulingEndpoints
{
    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder api)
    {
        GetAgendaEndpoint.Map(api.MapGroup("/agenda").WithTags("Agenda"));

        RouteGroupBuilder blocks = api.MapGroup("/schedule-blocks").WithTags("Agenda");
        CreateBlockEndpoint.Map(blocks);
        DeleteBlockEndpoint.Map(blocks);

        return api;
    }
}

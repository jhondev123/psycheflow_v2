using Psycheflow.Api.Features.Recurrences.CreateRecurrence;
using Psycheflow.Api.Features.Recurrences.EndRecurrence;
using Psycheflow.Api.Features.Recurrences.ExtendRecurrence;
using Psycheflow.Api.Features.Recurrences.ListRecurrences;

namespace Psycheflow.Api.Features.Recurrences;

public static class RecurrencesEndpoints
{
    public static IEndpointRouteBuilder MapRecurrencesEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/recurrences").WithTags("Recurrences");

        CreateRecurrenceEndpoint.Map(group);
        ListRecurrencesEndpoint.Map(group);
        ExtendRecurrenceEndpoint.Map(group);
        EndRecurrenceEndpoint.Map(group);

        return api;
    }
}

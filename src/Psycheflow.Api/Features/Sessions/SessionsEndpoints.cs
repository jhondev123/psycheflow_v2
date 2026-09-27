using Psycheflow.Api.Features.Sessions.CancelSession;
using Psycheflow.Api.Features.Sessions.CompleteSession;
using Psycheflow.Api.Features.Sessions.ConfirmSession;
using Psycheflow.Api.Features.Sessions.CreateSession;
using Psycheflow.Api.Features.Sessions.DeleteSession;
using Psycheflow.Api.Features.Sessions.GetSession;
using Psycheflow.Api.Features.Sessions.ListSessions;
using Psycheflow.Api.Features.Sessions.MarkNoShow;
using Psycheflow.Api.Features.Sessions.RescheduleSession;
using Psycheflow.Api.Features.Sessions.UpdateSession;

namespace Psycheflow.Api.Features.Sessions;

public static class SessionsEndpoints
{
    public static IEndpointRouteBuilder MapSessionsEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/sessions").WithTags("Sessions");

        CreateSessionEndpoint.Map(group);
        ListSessionsEndpoint.Map(group);
        GetSessionEndpoint.Map(group);
        UpdateSessionEndpoint.Map(group);
        ConfirmSessionEndpoint.Map(group);
        RescheduleSessionEndpoint.Map(group);
        CancelSessionEndpoint.Map(group);
        CompleteSessionEndpoint.Map(group);
        MarkNoShowEndpoint.Map(group);
        DeleteSessionEndpoint.Map(group);

        return api;
    }
}

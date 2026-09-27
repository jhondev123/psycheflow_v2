using Psycheflow.Api.Features.Psychologists.GetMyProfile;
using Psycheflow.Api.Features.Psychologists.GetPsychologist;
using Psycheflow.Api.Features.Psychologists.GetWorkingHours;
using Psycheflow.Api.Features.Psychologists.ListPsychologists;
using Psycheflow.Api.Features.Psychologists.SetWorkingHours;
using Psycheflow.Api.Features.Psychologists.UpdateProfile;

namespace Psycheflow.Api.Features.Psychologists;

public static class PsychologistsEndpoints
{
    public static IEndpointRouteBuilder MapPsychologistsEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/psychologists").WithTags("Psychologists");

        ListPsychologistsEndpoint.Map(group);
        GetMyProfileEndpoint.Map(group);
        GetPsychologistEndpoint.Map(group);
        UpdateProfileEndpoint.Map(group);
        GetWorkingHoursEndpoint.Map(group);
        SetWorkingHoursEndpoint.Map(group);

        return api;
    }
}

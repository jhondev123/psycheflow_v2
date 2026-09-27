using Psycheflow.Api.Features.Patients.CreatePatient;
using Psycheflow.Api.Features.Patients.GetPatient;
using Psycheflow.Api.Features.Patients.ListPatients;
using Psycheflow.Api.Features.Patients.UpdatePatient;

namespace Psycheflow.Api.Features.Patients;

public static class PatientsEndpoints
{
    public static IEndpointRouteBuilder MapPatientsEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/patients").WithTags("Patients");

        CreatePatientEndpoint.Map(group);
        ListPatientsEndpoint.Map(group);
        GetPatientEndpoint.Map(group);
        UpdatePatientEndpoint.Map(group);

        return api;
    }
}

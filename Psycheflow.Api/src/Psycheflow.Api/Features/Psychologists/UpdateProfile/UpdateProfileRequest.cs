namespace Psycheflow.Api.Features.Psychologists.UpdateProfile;

public sealed record UpdateProfileRequest(string FullName, string LicenseNumber, ApproachType Approach, string? Phone = null);

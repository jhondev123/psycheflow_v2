using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Users;

public static class UserErrors
{
    public static readonly Error OnlyAdminCanCreateAdmin = Error.Forbidden(
        "user.only_admin_can_create_admin", "Apenas um administrador pode cadastrar outro administrador.");
}

using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Psycheflow.Api.Common.Persistence;

public static class DbExceptionExtensions
{
    /// <summary>
    /// Violação de índice único no Postgres (23505). Cobre a corrida entre a checagem prévia no handler e o INSERT.
    /// </summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

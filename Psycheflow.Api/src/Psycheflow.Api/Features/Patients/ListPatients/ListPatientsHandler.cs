using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Patients.ListPatients;

/// <summary>
/// UC03 / RF002: lista paginada com busca (nome, e-mail ou CPF), filtro por status e por data da última sessão concluída.
/// </summary>
public sealed class ListPatientsHandler(AppDbContext db)
{
    public async Task<PagedResponse<PatientListItem>> Handle(ListPatientsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Patient> patients = db.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            if (Cpf.Create(term) is { IsSuccess: true } cpf)
            {
                patients = patients.Where(p => p.Cpf == cpf.Value);
            }
            else
            {
                string pattern = $"%{EscapeLike(term)}%";
                patients = patients.Where(p => EF.Functions.ILike(p.FullName, pattern) || EF.Functions.ILike(p.Email, pattern));
            }
        }

        if (query.Status is { } status)
        {
            patients = patients.Where(p => p.Status == status);
        }

        IQueryable<PatientRow> rows = patients.Select(p => new PatientRow
        {
            Id = p.Id,
            FullName = p.FullName,
            Cpf = p.Cpf,
            Email = p.Email,
            Phone = p.Phone,
            Status = p.Status,
            LastSessionDate = db.Sessions
                .Where(s => s.PatientId == p.Id && s.Status == SessionStatus.Completed)
                .Max(s => (DateOnly?)s.Schedule.Date),
        });

        if (query.LastSessionFrom is { } lastFrom)
        {
            rows = rows.Where(r => r.LastSessionDate >= lastFrom);
        }

        if (query.LastSessionTo is { } lastTo)
        {
            rows = rows.Where(r => r.LastSessionDate <= lastTo);
        }

        return await rows
            .OrderBy(r => r.FullName)
            .ToPagedResponseAsync(query.Page, query.PageSize, ToListItem, cancellationToken);
    }

    private static PatientListItem ToListItem(PatientRow row) =>
        new(row.Id, row.FullName, row.Cpf.Value, row.Email, row.Phone.Value, row.Status, row.LastSessionDate);

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);

    /// <summary>Projeção intermediária com inicializadores (o EF consegue filtrar e ordenar sobre ela).</summary>
    private sealed class PatientRow
    {
        public Guid Id { get; init; }

        public string FullName { get; init; } = string.Empty;

        public Cpf Cpf { get; init; } = null!;

        public string Email { get; init; } = string.Empty;

        public Phone Phone { get; init; } = null!;

        public PatientStatus Status { get; init; }

        public DateOnly? LastSessionDate { get; init; }
    }
}

using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Patients.ListPatients;

/// <summary>UC03 / RF002: lista paginada com busca (nome, e-mail ou CPF) e filtro por status.</summary>
public sealed class ListPatientsHandler(AppDbContext db)
{
    public async Task<PagedResponse<PatientListItem>> Handle(ListPatientsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Patient> patients = db.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            string pattern = $"%{EscapeLike(term)}%";

            if (Cpf.Create(term) is { IsSuccess: true } cpf)
            {
                patients = patients.Where(p => p.Cpf == cpf.Value);
            }
            else
            {
                patients = patients.Where(p => EF.Functions.ILike(p.FullName, pattern) || EF.Functions.ILike(p.Email, pattern));
            }
        }

        if (query.Status is { } status)
        {
            patients = patients.Where(p => p.Status == status);
        }

        return await patients
            .OrderBy(p => p.FullName)
            .Select(p => new PatientRow(p.Id, p.FullName, p.Cpf, p.Email, p.Phone, p.Status))
            .ToPagedResponseAsync(query.Page, query.PageSize, ToListItem, cancellationToken);
    }

    private static PatientListItem ToListItem(PatientRow row) =>
        new(row.Id, row.FullName, row.Cpf.Value, row.Email, row.Phone.Value, row.Status, LastSessionDate: null);

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);

    private sealed record PatientRow(Guid Id, string FullName, Cpf Cpf, string Email, Phone Phone, PatientStatus Status);
}

using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.Features.Payments.ListPayments;

/// <summary>UC12 / RF012: pagamentos por paciente, período da sessão e status.</summary>
public sealed class ListPaymentsHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<PagedResponse<PaymentResponse>>> Handle(ListPaymentsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Payment> payments = db.Payments.AsNoTracking();

        if (query.PsychologistId is { } psychologistId)
        {
            if (!currentUser.CanManagePsychologist(psychologistId))
            {
                return SchedulingErrors.CannotManageAgenda;
            }

            payments = payments.Where(p => p.Session!.PsychologistId == psychologistId);
        }
        else if (!currentUser.IsManagement())
        {
            Guid? own = currentUser.PsychologistId;
            payments = payments.Where(p => p.Session!.PsychologistId == own);
        }

        if (query.From is { } from)
        {
            payments = payments.Where(p => p.Session!.Schedule.Date >= from);
        }

        if (query.To is { } to)
        {
            payments = payments.Where(p => p.Session!.Schedule.Date <= to);
        }

        if (query.PatientId is { } patientId)
        {
            payments = payments.Where(p => p.Session!.PatientId == patientId);
        }

        if (query.Status is { } status)
        {
            payments = payments.Where(p => p.Status == status);
        }

        return await payments
            .OrderBy(p => p.Session!.Schedule.Date)
            .ThenBy(p => p.Session!.Schedule.StartTime)
            .Select(p => new PaymentResponse(
                p.Id,
                p.SessionId,
                p.Session!.PatientId,
                p.Session.Patient!.FullName,
                p.Session.PsychologistId,
                p.Session.Schedule.Date,
                p.Session.Schedule.StartTime,
                p.Session.Status,
                p.Amount,
                p.Status,
                p.Method,
                p.PaidAt,
                p.Notes,
                p.CancellationReason))
            .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);
    }
}

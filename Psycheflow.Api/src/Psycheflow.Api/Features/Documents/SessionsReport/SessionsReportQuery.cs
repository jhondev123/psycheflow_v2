using FluentValidation;
using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Documents.SessionsReport;

/// <param name="From">Início do período (obrigatório).</param>
/// <param name="PsychologistId">Psicólogo: o próprio por padrão; Admin/Manager sem filtro = clínica toda.</param>
public sealed record SessionsReportQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    SessionStatus? SessionStatus = null,
    PaymentStatus? PaymentStatus = null,
    Guid? PsychologistId = null);

public sealed class SessionsReportQueryValidator : AbstractValidator<SessionsReportQuery>
{
    public SessionsReportQueryValidator()
    {
        RuleFor(x => x.From).NotNull().WithMessage("Informe a data inicial do período.");
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).WithMessage("A data final deve ser igual ou posterior à inicial.")
            .When(x => x.To is not null && x.From is not null);
    }
}

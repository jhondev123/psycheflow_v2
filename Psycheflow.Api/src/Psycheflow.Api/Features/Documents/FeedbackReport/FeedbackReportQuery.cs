using FluentValidation;

namespace Psycheflow.Api.Features.Documents.FeedbackReport;

/// <param name="PatientId">Obrigatório (RN-64).</param>
/// <param name="From">Início do período (obrigatório); o fim é opcional.</param>
public sealed record FeedbackReportQuery(Guid? PatientId = null, DateOnly? From = null, DateOnly? To = null);

public sealed class FeedbackReportQueryValidator : AbstractValidator<FeedbackReportQuery>
{
    public FeedbackReportQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Informe o paciente.");
        RuleFor(x => x.From).NotNull().WithMessage("Informe a data inicial do período.");
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).WithMessage("A data final deve ser igual ou posterior à inicial.")
            .When(x => x.To is not null && x.From is not null);
    }
}

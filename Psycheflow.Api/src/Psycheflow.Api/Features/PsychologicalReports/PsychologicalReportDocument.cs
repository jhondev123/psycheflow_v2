using Psycheflow.Api.Features.Documents.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Psycheflow.Api.Features.PsychologicalReports;

/// <param name="SessionSummary">Resumo das sessões (quando o psicólogo optou por incluí-lo).</param>
public sealed record PsychologicalReportModel(
    DocumentHeaderData Header,
    PsychologicalReportTemplate Template,
    PsychologicalReportStatus Status,
    string PatientName,
    string PatientCpf,
    ReportSections Sections,
    string? SessionSummary);

/// <summary>Laudo/relatório psicológico na estrutura da Resolução CFP 06/2019. Rascunhos saem com marca d'água.</summary>
public sealed class PsychologicalReportDocument(PsychologicalReportModel model) : IDocument
{
    private const string Empty = "(não preenchido)";

    public static string TitleOf(PsychologicalReportTemplate template) => template switch
    {
        PsychologicalReportTemplate.PsychologicalReport => "Laudo psicológico",
        PsychologicalReportTemplate.PsychologicalStatement => "Relatório psicológico",
        _ => "Documento psicológico",
    };

    public void Compose(IDocumentContainer container) =>
        container.Page(page =>
        {
            PdfLayout.ApplyPage(page);
            page.Header().Element(c => PdfLayout.Header(c, model.Header, TitleOf(model.Template)));

            if (model.Status == PsychologicalReportStatus.Draft)
            {
                page.Foreground().AlignMiddle().AlignCenter().Rotate(-30)
                    .Text("RASCUNHO").FontSize(80).Bold().FontColor(Colors.Grey.Lighten3);
            }

            page.Content().Column(column =>
            {
                column.Spacing(10);
                column.Item().Element(c => Section(c, "1. Identificação", identification =>
                {
                    identification.Item().Text(text =>
                    {
                        text.Span("Autor(a): ").SemiBold();
                        text.Span($"{model.Header.ProfessionalName} · CRP {model.Header.LicenseNumber}");
                    });
                    identification.Item().Text(text =>
                    {
                        text.Span("Interessado(a): ").SemiBold();
                        text.Span($"{model.PatientName} · CPF {DocumentLabels.FormatCpf(model.PatientCpf)}");
                    });
                    identification.Item().Text(text =>
                    {
                        text.Span("Assunto/finalidade: ").SemiBold();
                        text.Span(model.Sections.Purpose ?? Empty);
                    });
                }));
                column.Item().Element(c => Paragraph(c, "2. Descrição da demanda", model.Sections.Demand));
                column.Item().Element(c => Section(c, "3. Procedimento", procedure =>
                {
                    procedure.Item().Text(model.Sections.Procedure ?? Empty);
                    if (model.SessionSummary is not null)
                    {
                        procedure.Item().PaddingTop(4).Text(model.SessionSummary).Italic().FontColor(PdfLayout.Muted);
                    }
                }));
                column.Item().Element(c => Paragraph(c, "4. Análise", model.Sections.Analysis));
                column.Item().Element(c => Paragraph(c, "5. Conclusão", model.Sections.Conclusion));
                column.Item().PaddingTop(10).AlignRight().Text(PdfLayout.LongDate(DateOnly.FromDateTime(model.Header.IssuedAt.DateTime)));
                column.Item().Element(c => PdfLayout.Signature(c, model.Header));
            });
            page.Footer().Element(PdfLayout.Footer);
        });

    private static void Paragraph(IContainer container, string title, string? content) =>
        Section(container, title, column => column.Item().Text(content ?? Empty).Justify());

    private static void Section(IContainer container, string title, Action<ColumnDescriptor> body) =>
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(11).SemiBold().FontColor(PdfLayout.Primary);
            column.Item().PaddingTop(3).Column(body);
        });
}

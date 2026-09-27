using Psycheflow.Api.Features.Documents.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Psycheflow.Api.Features.Documents.Attendance;

public sealed record AttendanceModel(
    DocumentHeaderData Header,
    string PatientName,
    string PatientCpf,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End);

/// <summary>Declaração de comparecimento (CFP 06/2019): informa só o comparecimento, sem dados clínicos.</summary>
public sealed class AttendanceDocument(AttendanceModel model) : IDocument
{
    public void Compose(IDocumentContainer container) =>
        container.Page(page =>
        {
            PdfLayout.ApplyPage(page);
            page.Header().Element(c => PdfLayout.Header(c, model.Header, "Declaração de comparecimento"));
            page.Content().PaddingTop(20).Column(column =>
            {
                column.Spacing(14);
                column.Item().Text(text =>
                {
                    text.Justify();
                    text.Span("Declaro, para os devidos fins, que ");
                    text.Span(model.PatientName).SemiBold();
                    text.Span($", inscrito(a) no CPF {DocumentLabels.FormatCpf(model.PatientCpf)}, compareceu a atendimento psicológico no dia ");
                    text.Span(PdfLayout.LongDate(model.Date)).SemiBold();
                    text.Span($", das {PdfLayout.Time(model.Start)} às {PdfLayout.Time(model.End)}.");
                });
                column.Item().PaddingTop(10).AlignRight().Text(PdfLayout.LongDate(DateOnly.FromDateTime(model.Header.IssuedAt.DateTime)));
                column.Item().Element(c => PdfLayout.Signature(c, model.Header));
            });
            page.Footer().Element(PdfLayout.Footer);
        });
}

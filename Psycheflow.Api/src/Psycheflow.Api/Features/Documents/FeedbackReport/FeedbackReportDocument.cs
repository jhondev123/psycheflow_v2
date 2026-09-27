using Psycheflow.Api.Features.Documents.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Psycheflow.Api.Features.Documents.FeedbackReport;

public sealed record FeedbackReportRow(DateOnly Date, TimeOnly Start, int Score, string? Comment);

public sealed record FeedbackReportModel(
    DocumentHeaderData Header,
    string PatientName,
    string PatientCpf,
    int? PatientAge,
    DateOnly From,
    DateOnly? To,
    IReadOnlyList<FeedbackReportRow> Rows)
{
    public decimal? Average => Rows.Count == 0 ? null : Math.Round((decimal)Rows.Average(r => r.Score), 1);
}

/// <summary>RF015-B: feedbacks (nota 0–10) do paciente por sessão no período.</summary>
public sealed class FeedbackReportDocument(FeedbackReportModel model) : IDocument
{
    private static readonly string[] ColumnTitles = ["Data", "Hora", "Nota", "Comentário"];

    public void Compose(IDocumentContainer container) =>
        container.Page(page =>
        {
            PdfLayout.ApplyPage(page);
            page.Header().Element(c => PdfLayout.Header(c, model.Header, "Relatório de feedback do paciente", Period()));
            page.Content().Column(column =>
            {
                column.Spacing(12);
                column.Item().Border(1).BorderColor(PdfLayout.Border).Padding(10).Row(row =>
                {
                    row.RelativeItem(3).Column(c =>
                    {
                        c.Item().Text("Paciente").FontSize(8).FontColor(PdfLayout.Muted);
                        c.Item().Text(model.PatientName).SemiBold();
                        c.Item().Text($"CPF {DocumentLabels.FormatCpf(model.PatientCpf)}" + (model.PatientAge is { } age ? $" · {age} anos" : string.Empty)).FontSize(9);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Sessões avaliadas").FontSize(8).FontColor(PdfLayout.Muted);
                        c.Item().Text(model.Rows.Count.ToString(PdfLayout.Culture)).SemiBold();
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Média").FontSize(8).FontColor(PdfLayout.Muted);
                        c.Item().Text(model.Average?.ToString("0.0", PdfLayout.Culture) ?? "—").SemiBold();
                    });
                });

                if (model.Rows.Count == 0)
                {
                    column.Item().PaddingTop(20).AlignCenter().Text("Nenhum feedback registrado no período.").FontColor(PdfLayout.Muted);
                    return;
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(45);
                        columns.ConstantColumn(45);
                        columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        foreach (string title in ColumnTitles)
                        {
                            header.Cell().Background(PdfLayout.Primary).Padding(4).Text(title).FontColor(Colors.White).FontSize(9).SemiBold();
                        }
                    });
                    foreach (FeedbackReportRow row in model.Rows)
                    {
                        table.Cell().Element(Cell).Text(PdfLayout.Date(row.Date));
                        table.Cell().Element(Cell).Text(PdfLayout.Time(row.Start));
                        table.Cell().Element(Cell).Text(row.Score.ToString(PdfLayout.Culture)).SemiBold();
                        table.Cell().Element(Cell).Text(row.Comment ?? "—");
                    }
                });
            });
            page.Footer().Element(PdfLayout.Footer);
        });

    private string Period() => model.To is { } to
        ? $"Período: {PdfLayout.Date(model.From)} a {PdfLayout.Date(to)}"
        : $"Período: a partir de {PdfLayout.Date(model.From)}";

    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(PdfLayout.Border).PaddingVertical(4).PaddingHorizontal(3).DefaultTextStyle(s => s.FontSize(9));
}

using System.Globalization;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Psycheflow.Api.Features.Documents.Pdf;

public static class PdfSetup
{
    /// <summary>Licença Community do QuestPDF (gratuita para receita anual abaixo de US$ 1 mi) — chamada na inicialização.</summary>
    public static void Configure() => Settings.License = LicenseType.Community;
}

/// <summary>Dados do cabeçalho comum: clínica, profissional responsável e momento da emissão (horário local).</summary>
/// <param name="LicenseNumber">CRP do profissional; nulo em relatórios da clínica toda.</param>
public sealed record DocumentHeaderData(string ClinicName, string ProfessionalName, string? LicenseNumber, DateTimeOffset IssuedAt);

/// <summary>Identidade visual e formatação (pt-BR) compartilhadas por todos os documentos.</summary>
public static class PdfLayout
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    public static readonly Color Primary = Color.FromHex("#3B5B8C");
    public static readonly Color Muted = Colors.Grey.Darken1;
    public static readonly Color Border = Colors.Grey.Lighten2;

    public static void ApplyPage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.DefaultTextStyle(style => style.FontSize(10.5f).LineHeight(1.35f));
    }

    public static void Header(IContainer container, DocumentHeaderData header, string title, string? subtitle = null) =>
        container.PaddingBottom(14).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(header.ClinicName).FontSize(14).SemiBold().FontColor(Primary);
                    left.Item().Text(ProfessionalLine(header)).FontSize(9).FontColor(Muted);
                });
                row.ConstantItem(150).AlignRight().Text($"Emitido em {DateTime(header.IssuedAt)}").FontSize(8).FontColor(Muted);
            });
            column.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Primary);
            column.Item().AlignCenter().Text(title.ToUpper(Culture)).FontSize(13).Bold();
            if (subtitle is not null)
            {
                column.Item().AlignCenter().Text(subtitle).FontSize(9).FontColor(Muted);
            }
        });

    public static void Footer(IContainer container) =>
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
            text.Span("Psycheflow · página ");
            text.CurrentPageNumber();
            text.Span(" de ");
            text.TotalPages();
        });

    /// <summary>Linha de assinatura do profissional.</summary>
    public static void Signature(IContainer container, DocumentHeaderData header) =>
        container.PaddingTop(40).AlignCenter().Width(260).Column(column =>
        {
            column.Item().LineHorizontal(0.75f);
            column.Item().AlignCenter().Text(header.ProfessionalName).SemiBold();
            if (header.LicenseNumber is not null)
            {
                column.Item().AlignCenter().Text($"Psicólogo(a) · CRP {header.LicenseNumber}").FontSize(9).FontColor(Muted);
            }
        });

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Culture);

    public static string LongDate(DateOnly date) => date.ToString("d 'de' MMMM 'de' yyyy", Culture);

    public static string Time(TimeOnly time) => time.ToString("HH:mm", Culture);

    public static string DateTime(DateTimeOffset moment) => moment.ToString("dd/MM/yyyy HH:mm", Culture);

    public static string Money(decimal amount) => amount.ToString("C", Culture);

    private static string ProfessionalLine(DocumentHeaderData header) =>
        header.LicenseNumber is null ? header.ProfessionalName : $"{header.ProfessionalName} · CRP {header.LicenseNumber}";
}

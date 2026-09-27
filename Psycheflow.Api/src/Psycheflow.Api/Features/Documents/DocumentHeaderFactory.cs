using System.Globalization;
using System.Text;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Common.Time;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Documents.Pdf;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Documents;

/// <summary>PDF gerado: conteúdo e nome de arquivo sugerido.</summary>
public sealed record PdfFile(byte[] Content, string FileName)
{
    public const string ContentType = "application/pdf";

    /// <summary>Nome de arquivo seguro: sem acentos, minúsculo, com hífens.</summary>
    public static string BuildFileName(params string[] parts)
    {
        string joined = string.Join('-', parts);
        var builder = new StringBuilder();
        foreach (char c in joined.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsAsciiLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        }

        string collapsed = string.Join('-', builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return $"{collapsed}.pdf";
    }
}

public static class DocumentErrors
{
    public static readonly Error ReceiptRequiresPaidPayment = Error.Conflict(
        "document.receipt_requires_paid_payment", "O recibo só pode ser emitido para pagamentos recebidos.");

    public static readonly Error AttendanceRequiresCompletedSession = Error.Conflict(
        "document.attendance_requires_completed_session", "A declaração de comparecimento só pode ser emitida para sessões concluídas.");

    public static readonly Error OnlyPsychologists = Error.Forbidden(
        "document.only_psychologists", "Somente psicólogos podem emitir documentos com dados clínicos.");
}

/// <summary>Monta o cabeçalho comum dos documentos com o horário local da clínica.</summary>
public sealed class DocumentHeaderFactory(AppDbContext db, ICurrentUser currentUser, ClinicClock clock)
{
    public const string AllProfessionals = "Todos os profissionais";

    /// <param name="professional">Profissional responsável (com <c>User</c> carregado) ou nulo para a clínica toda.</param>
    public async Task<DocumentHeaderData> CreateAsync(Psychologist? professional, CancellationToken cancellationToken)
    {
        Company company = await db.FindCurrentAsync(currentUser, cancellationToken)
            ?? throw new InvalidOperationException("Empresa do usuário logado não encontrada.");

        DateTimeOffset issuedAt = TimeZoneInfo.ConvertTime(clock.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(company.Settings.TimeZone));

        return new DocumentHeaderData(
            company.Name,
            professional?.User?.FullName ?? AllProfessionals,
            professional?.LicenseNumber.Value,
            issuedAt);
    }
}

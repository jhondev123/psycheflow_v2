using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.MedicalRecords;

/// <summary>
/// Regras de anexos (RN-65): só PDF, JPG e PNG até 10 MB. O tipo é conferido pela assinatura do arquivo
/// (magic bytes), não pela extensão ou pelo Content-Type enviado pelo cliente.
/// </summary>
public static class AttachmentPolicy
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const int HeaderLength = 8;

    private static readonly (byte[] Signature, string ContentType)[] Signatures =
    [
        ("%PDF-"u8.ToArray(), "application/pdf"),
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "image/png"),
        ([0xFF, 0xD8, 0xFF], "image/jpeg"),
    ];

    public static readonly Error Missing = Error.Validation("attachment.missing", "Envie um arquivo.", "file");

    public static readonly Error TooLarge = Error.Validation(
        "attachment.too_large", "O arquivo deve ter no máximo 10 MB.", "file");

    public static readonly Error UnsupportedType = Error.Validation(
        "attachment.unsupported_type", "Envie um arquivo PDF, JPG ou PNG.", "file");

    public static string? DetectContentType(ReadOnlySpan<byte> header)
    {
        foreach ((byte[] signature, string contentType) in Signatures)
        {
            if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
            {
                return contentType;
            }
        }

        return null;
    }

    /// <summary>Mantém só o nome do arquivo (sem caminho), para exibição e download.</summary>
    public static string SafeFileName(string? fileName)
    {
        string name = (fileName ?? string.Empty).Replace('\\', '/').Split('/').Last().Trim();
        return name.Length == 0 ? "arquivo" : name[..Math.Min(name.Length, 200)];
    }
}

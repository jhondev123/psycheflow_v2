using Psycheflow.Api.Features.MedicalRecords;

namespace Psycheflow.Api.UnitTests.Features.MedicalRecords;

public sealed class AttachmentPolicyTests
{
    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, "application/pdf")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 }, "image/jpeg")]
    public void DetectContentType_RecognizesAllowedSignatures(byte[] header, string expected) =>
        AttachmentPolicy.DetectContentType(header).ShouldBe(expected);

    [Theory]
    [InlineData(new byte[] { 0x4D, 0x5A, 0x90, 0x00 })]
    [InlineData(new byte[] { 0x25, 0x50 })]
    [InlineData(new byte[0])]
    public void DetectContentType_UnknownOrTruncated_IsNull(byte[] header) =>
        AttachmentPolicy.DetectContentType(header).ShouldBeNull();

    [Theory]
    [InlineData("exame.pdf", "exame.pdf")]
    [InlineData("C:\\fakepath\\laudo antigo.pdf", "laudo antigo.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("  ", "arquivo")]
    public void SafeFileName_KeepsOnlyTheName(string input, string expected) =>
        AttachmentPolicy.SafeFileName(input).ShouldBe(expected);

    [Fact]
    public void MedicalRecord_Create_TrimsTitle() =>
        MedicalRecord.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "  Anamnese ", "Conteúdo").Title.ShouldBe("Anamnese");
}

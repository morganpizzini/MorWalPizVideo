using System.Buffers.Binary;
using FreezerAnalysis.Api.Services;

namespace FreezerAnalysis.Api.Tests;

public sealed class FreezerDocumentRendererTests
{
    [Fact]
    public async Task Renderer_produces_a5_a4_and_exact_phone_png_artifacts()
    {
        var renderer = new FreezerDocumentRenderer();
        var documents = await renderer.RenderAsync(new FreezerCardPayload(
            "Gara sintetica",
            "2026-10-29",
            "PRODUCTION",
            "Minor",
            [
                new FreezerCardStage(1, 12, 4.2, 3.8, 4.6, "Punta alla precisione.")
            ],
            null,
            null,
            null,
            [],
            []),
            CancellationToken.None);

        Assert.Equal(FreezerDocumentRenderer.RendererVersion, documents.RendererVersion);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(documents.A5Pdf, 0, 5));
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(documents.A4FoldPdf, 0, 5));
        AssertPngDimensions(documents.PhoneFrontPng);
        AssertPngDimensions(documents.PhoneBackPng);
    }

    [Fact]
    public async Task Renderer_rejects_content_that_overflows_the_declared_readable_area()
    {
        var renderer = new FreezerDocumentRenderer();
        var payload = new FreezerCardPayload(
            new string('G', 500),
            "2026-10-29",
            "PRODUCTION",
            "Minor",
            Enumerable.Range(1, 34)
                .Select(stage => new FreezerCardStage(stage, 100, 4.2, 3.8, 4.6, new string('P', 260)))
                .ToArray(),
            null,
            null,
            null,
            [],
            []);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            renderer.RenderAsync(payload, CancellationToken.None));
    }

    private static void AssertPngDimensions(byte[] png)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.True(png.AsSpan(0, signature.Length).SequenceEqual(signature));
        Assert.Equal(FreezerDocumentRenderer.PhoneWidth, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal(FreezerDocumentRenderer.PhoneHeight, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
    }
}

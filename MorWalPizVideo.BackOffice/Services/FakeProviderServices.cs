using System.Security.Cryptography;
using System.Text;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class FaqCandidateAiProviderMock : IFaqCandidateAiProvider
{
    public Task<IReadOnlyList<FaqCandidateDraft>> GenerateAsync(
        IReadOnlyList<FaqCandidateSource> sources,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<FaqCandidateDraft>>([]);
    }
}

public sealed class ImageGenerationServiceMock : IImageGenerationService
{
    public Task<string> GenerateCreatorImageAsync(
        string creatorName,
        string fontStyle = "Arial",
        int fontSize = 48,
        string textColor = "#FFFFFF",
        string outlineColor = "#000000",
        int outlineThickness = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorName);
        var input = $"{creatorName}|{fontStyle}|{fontSize}|{textColor}|{outlineColor}|{outlineThickness}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
        return Task.FromResult($"fake/{hash}.png");
    }

    public Task<Stream?> GetExistingImageAsync(string imageName)
        => Task.FromResult<Stream?>(null);
}
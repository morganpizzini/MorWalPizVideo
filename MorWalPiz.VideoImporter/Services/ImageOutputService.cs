using System.Text.RegularExpressions;
using System.IO;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Services;

public interface IImageOutputService
{
    Task<IReadOnlyList<string>> SaveAsync(IReadOnlyList<GeneratedImage> images, string outputDirectory, string name, CancellationToken cancellationToken);
}

public sealed class ImageOutputService : IImageOutputService
{
    public async Task<IReadOnlyList<string>> SaveAsync(IReadOnlyList<GeneratedImage> images, string outputDirectory, string name, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var safeName = Regex.Replace(name.Trim(), "[^a-zA-Z0-9._-]+", "_").Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "generated-image";
        var paths = new List<string>();
        foreach (var image in images)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = await WriteNewFileAsync(outputDirectory, safeName, image, cancellationToken);
            paths.Add(path);
        }
        return paths;
    }

    private static async Task<string> WriteNewFileAsync(string directory, string name, GeneratedImage image, CancellationToken cancellationToken)
    {
        var index = 0;
        while (true)
        {
            var suffix = index == 0 ? string.Empty : $"-{index}";
            var path = Path.Combine(directory, $"{name}{suffix}.{image.Extension}");
            FileStream? stream = null;
            try
            {
                stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException) { index++; }
            if (stream is null) continue;
            using (stream)
            {
                await stream.WriteAsync(image.Content, cancellationToken);
            }
            return path;
        }
    }
}
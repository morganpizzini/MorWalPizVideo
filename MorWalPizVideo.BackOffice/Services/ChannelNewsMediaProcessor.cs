using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MorWalPizVideo.BackOffice.Services;

public sealed record PreparedChannelNewsImage(
    MemoryStream Content,
    int Width,
    int Height,
    string ContentType,
    string Extension);

public static class ChannelNewsMediaProcessor
{
    public static async Task<PreparedChannelNewsImage> PrepareImageAsync(Stream input)
    {
        using var image = await Image.LoadAsync(input);
        image.Mutate(context => context.AutoOrient());
        if (image.Width >= image.Height)
            ResizeToBounds(image, 1920, 1080);
        else
            ResizeToBounds(image, 1080, 1920);
        var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 90 });
        output.Position = 0;
        return new PreparedChannelNewsImage(output, image.Width, image.Height, "image/jpeg", ".jpg");
    }

    public static async Task<PreparedChannelNewsImage> PrepareImageAsIsAsync(Stream input)
    {
        if (!input.CanSeek)
            throw new InvalidOperationException("The image stream must be seekable to preserve the original upload.");

        var original = new MemoryStream();
        await input.CopyToAsync(original);
        original.Position = 0;

        var format = await Image.DetectFormatAsync(original);
        if (format is null)
            throw new InvalidDataException("The uploaded image format is not supported.");

        var imageInfo = await Image.IdentifyAsync(original);
        original.Position = 0;

        var output = new MemoryStream();
        await original.CopyToAsync(output);
        output.Position = 0;

        return new PreparedChannelNewsImage(output, imageInfo.Width, imageInfo.Height, format.DefaultMimeType, GetFormatExtension(format.DefaultMimeType));
    }

    public static async Task<PreparedChannelNewsImage> PrepareLogoAsync(Stream input)
    {
        var signature = new byte[8];
        if (await input.ReadAsync(signature) != signature.Length ||
            !signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            throw new InvalidDataException("The channel logo must be a PNG image.");
        }
        input.Position = 0;
        using var image = await Image.LoadAsync(input);
        image.Mutate(context => context.AutoOrient());
        if (image.Width > 500)
        {
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(500, 500)
            }));
        }

        var output = new MemoryStream();
        await image.SaveAsPngAsync(output, new PngEncoder());
        output.Position = 0;
        return new PreparedChannelNewsImage(output, image.Width, image.Height, "image/png", ".png");
    }

    private static void ResizeToBounds(Image image, int maxLongSide, int maxShortSide)
    {
        var longSide = Math.Max(image.Width, image.Height);
        if (longSide <= maxLongSide)
            return;

        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxLongSide, maxShortSide)
        }));
    }

    private static string GetFormatExtension(string mimeType)
        => mimeType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            _ => ".bin"
        };
}

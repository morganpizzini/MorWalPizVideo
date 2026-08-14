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
}

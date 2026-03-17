using Microsoft.AspNetCore.Mvc;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace MorWalPizVideo.BackOffice.Controllers;

public class QRCodeController : ApplicationControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateQRCode(IFormFile logoFile, string data)
    {
        try
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

                    using var qrCodeImage = Image.Load(qrCodeAsPngByteArr);

                    if (logoFile != null && logoFile.Length > 0)
                    {
                        using var logo = await LoadImageFromFormFile(logoFile);
                        if (logo != null)
                        {
                            int logoSize = qrCodeImage.Width / 5;
                            
                            // Resize logo to fit in the center of QR code
                            logo.Mutate(x => x.Resize(logoSize, logoSize));

                            // Calculate center position
                            int centerX = (qrCodeImage.Width - logoSize) / 2;
                            int centerY = (qrCodeImage.Height - logoSize) / 2;

                            // Draw logo on QR code
                            qrCodeImage.Mutate(ctx => ctx.DrawImage(logo, new Point(centerX, centerY), 1f));
                        }
                    }

                    using (var outputStream = new MemoryStream())
                    {
                        await qrCodeImage.SaveAsync(outputStream, new PngEncoder());
                        return File(outputStream.ToArray(), "image/png", "qrcode.png");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating QR code: {ex.Message}");
        }
    }

    private async Task<Image?> LoadImageFromFormFile(IFormFile file)
    {
        try
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                return await Image.LoadAsync(stream);
            }
        }
        catch
        {
            return null;
        }
    }
}

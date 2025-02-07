using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace MorWalPizVideo.BackOffice.Controllers;

public class QRCodeController : ApplicationController
{
    [HttpPost]
    public async Task<IActionResult> GenerateQRCode(IFormFile logoFile, string data)
    {
        try
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new BitmapByteQRCode(qrCodeData))
                {
                    byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

                    using var ms = new MemoryStream(qrCodeAsPngByteArr);
                    
                    Bitmap qrCodeImage = new Bitmap(ms);

                    if (logoFile != null && logoFile.Length > 0)
                    {
                        Bitmap logo = await ConvertIFormFileToBitmap(logoFile);
                        if (logo != null)
                        {
                            int logoSize = qrCodeImage.Width / 5;
                            Bitmap resizedLogo = new Bitmap(logo, new Size(logoSize, logoSize));

                            using (Graphics graphics = Graphics.FromImage(qrCodeImage))
                            {
                                int centerX = (qrCodeImage.Width - logoSize) / 2;
                                int centerY = (qrCodeImage.Height - logoSize) / 2;
                                graphics.DrawImage(resizedLogo, new Point(centerX, centerY));
                            }
                        }
                    }

                    using (MemoryStream ms1 = new MemoryStream())
                    {
                        qrCodeImage.Save(ms1, ImageFormat.Png);
                        return File(ms.ToArray(), "image/png", "qrcode.png");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating QR code: {ex.Message}");
        }
    }

    private async Task<Bitmap> ConvertIFormFileToBitmap(IFormFile file)
    {
        try
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                return new Bitmap(stream);
            }
        }
        catch
        {
            return null;
        }
    }
}

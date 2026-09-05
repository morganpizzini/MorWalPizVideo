using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.Domain;

public static class NewsletterCrypto
{
    public static string Decrypt(string ciphertext, string configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey))
            throw new InvalidOperationException("Newsletter:EncryptionKey must be configured before reading subscriber data.");
        var payload = Convert.FromBase64String(ciphertext);
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        aes.IV = payload[..(aes.BlockSize / 8)];
        using var decryptor = aes.CreateDecryptor();
        return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(payload, aes.BlockSize / 8, payload.Length - aes.BlockSize / 8));
    }
}

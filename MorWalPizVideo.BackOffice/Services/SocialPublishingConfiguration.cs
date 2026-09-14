using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class SocialPublishingOptions
{
    public string EncryptionKey { get; set; } = string.Empty;
}

public interface ISocialPublishingSecretProtector
{
    string Protect(string plaintext, string channelId, string provider);
    string Unprotect(string ciphertext, string channelId, string provider);
}

public sealed class SocialPublishingSecretProtector(IOptions<SocialPublishingOptions> options)
    : ISocialPublishingSecretProtector
{
    private const byte FormatVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Protect(string plaintext, string channelId, string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(GetKey(), TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, GetAssociatedData(channelId, provider));

        var payload = new byte[1 + NonceSize + TagSize + ciphertext.Length];
        payload[0] = FormatVersion;
        nonce.CopyTo(payload, 1);
        tag.CopyTo(payload, 1 + NonceSize);
        ciphertext.CopyTo(payload, 1 + NonceSize + TagSize);
        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string ciphertext, string channelId, string provider)
    {
        var payload = Convert.FromBase64String(ciphertext);
        if (payload.Length < 1 + NonceSize + TagSize || payload[0] != FormatVersion)
        {
            throw new CryptographicException("The social publishing credential has an unsupported format.");
        }

        var nonce = payload.AsSpan(1, NonceSize);
        var tag = payload.AsSpan(1 + NonceSize, TagSize);
        var encrypted = payload.AsSpan(1 + NonceSize + TagSize);
        var plaintext = new byte[encrypted.Length];

        using var aes = new AesGcm(GetKey(), TagSize);
        aes.Decrypt(nonce, encrypted, tag, plaintext, GetAssociatedData(channelId, provider));
        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetKey()
    {
        if (string.IsNullOrWhiteSpace(options.Value.EncryptionKey))
        {
            throw new InvalidOperationException("SocialPublishing:EncryptionKey must be configured before managing social publishing credentials.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(options.Value.EncryptionKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("SocialPublishing:EncryptionKey must be a base64-encoded 32-byte key.", exception);
        }

        return key.Length == 32
            ? key
            : throw new InvalidOperationException("SocialPublishing:EncryptionKey must be a base64-encoded 32-byte key.");
    }

    private static byte[] GetAssociatedData(string channelId, string provider) =>
        Encoding.UTF8.GetBytes($"{channelId}\n{provider.Trim().ToLowerInvariant()}");
}

public sealed record SocialPublishingCredentials(string DestinationId, string Credential);

public sealed class SocialProviderNotConfiguredException(string provider)
    : InvalidOperationException($"{provider} publishing is not configured for the selected channel.")
{
    public string Provider { get; } = provider;
}

public interface ISelectedChannelPublishingConfigurationAccessor
{
    SocialPublishingCredentials Get(string provider);
}

public sealed class SelectedChannelPublishingConfigurationAccessor(
    IHttpContextAccessor httpContextAccessor,
    ISocialPublishingSecretProtector secretProtector)
    : ISelectedChannelPublishingConfigurationAccessor
{
    public SocialPublishingCredentials Get(string provider)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Social publishing requires an active HTTP request.");
        var channelContext = httpContext.GetChannelContext();
        var configuration = GetProviderConfiguration(channelContext.Channel, provider);

        if (string.IsNullOrWhiteSpace(configuration.DestinationId) ||
            string.IsNullOrWhiteSpace(configuration.CredentialCiphertext))
        {
            throw new SocialProviderNotConfiguredException(provider);
        }

        return new SocialPublishingCredentials(
            configuration.DestinationId,
            secretProtector.Unprotect(
                configuration.CredentialCiphertext,
                channelContext.ChannelId,
                provider));
    }

    private static SocialPublishingProviderConfiguration GetProviderConfiguration(YTChannel channel, string provider) =>
        provider.Trim().ToLowerInvariant() switch
        {
            "telegram" => channel.SocialPublishing?.Telegram ?? new SocialPublishingProviderConfiguration(),
            "discord" => channel.SocialPublishing?.Discord ?? new SocialPublishingProviderConfiguration(),
            "facebook" => channel.SocialPublishing?.Facebook ?? new SocialPublishingProviderConfiguration(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported social publishing provider.")
        };
}
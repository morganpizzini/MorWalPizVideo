using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public interface INewsletterService
{
    Task<string?> SubscribeAsync(string channelId, string email, string language, CancellationToken cancellationToken = default);
    Task<string?> ConfirmAsync(string channelId, string token, CancellationToken cancellationToken = default);
    Task<NewsletterUnsubscribeResult> UnsubscribeAsync(string channelId, string token, CancellationToken cancellationToken = default);

    Task<Newsletter> CreateAsync(Newsletter newsletter, CancellationToken cancellationToken = default);
    Task<Newsletter?> TransitionAsync(string channelId, string id, NewsletterState target, CancellationToken cancellationToken = default);
}

public sealed class NewsletterService(
    INewsletterRepository newsletterRepository,
    INewsletterUserRepository userRepository,
    INewsletterEmailService emailService,
    INewsletterEventRepository eventRepository,
    IConfiguration configuration) : INewsletterService
{
    private static readonly Regex EmailPattern = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<string?> SubscribeAsync(string channelId, string email, string language, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(channelId) || !EmailPattern.IsMatch(normalizedEmail) || language is not ("IT" or "ENG"))
            return null;

        var emailHash = Hash(normalizedEmail);
        var existing = (await userRepository.GetItemsAsync(user => user.ChannelId == channelId && user.EmailHash == emailHash)).FirstOrDefault();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Hash(token);
        var user = existing is null
            ? new NewsletterUser(channelId, emailHash, Encrypt(normalizedEmail, configuration["Newsletter:EncryptionKey"] ?? string.Empty), language)
            : existing with { Language = language, Status = NewsletterUserStatus.PendingConfirmation };
        user = user with { ConfirmationTokenHash = tokenHash, ConfirmationExpiresAt = DateTime.UtcNow.AddHours(24) };
        if (existing is null) await userRepository.AddItemAsync(user);
        else await userRepository.UpdateItemAsync(user);
        var confirmationLink = BuildPublicLink("confirm", channelId, token, configuration);
        await emailService.SendAsync(normalizedEmail, "Confirm your newsletter subscription", $"<p>Confirm your subscription:</p><p><a href=\"{confirmationLink}\">Confirm subscription</a></p>", $"confirm:{channelId}:{user.Id}", cancellationToken);
        return token;
    }

    public async Task<string?> ConfirmAsync(string channelId, string token, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.ConsumeConfirmationAsync(channelId, Hash(token), DateTime.UtcNow, cancellationToken);
        if (user is null) return null;
        var unsubscribeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await userRepository.UpdateItemAsync(user with
        {
            UnsubscribeTokenHash = Hash(unsubscribeToken),
            UnsubscribeTokenCiphertext = Encrypt(unsubscribeToken, configuration["Newsletter:EncryptionKey"] ?? string.Empty),
            UnsubscribeExpiresAt = DateTime.UtcNow.AddYears(1)
        });
        return unsubscribeToken;
    }

    public async Task<NewsletterUnsubscribeResult> UnsubscribeAsync(string channelId, string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(token);
        var now = DateTime.UtcNow;
        var consumed = await userRepository.ConsumeUnsubscribeAsync(channelId, tokenHash, now, cancellationToken);
        if (consumed is null)
        {
            var matchingUser = (await userRepository.GetItemsAsync(user => user.ChannelId == channelId && user.UnsubscribeTokenHash == tokenHash)).FirstOrDefault();
            if (matchingUser is not null && matchingUser.UnsubscribeExpiresAt <= now)
                return NewsletterUnsubscribeResult.Expired;

            var consumedEventId = $"unsubscribe:{channelId}:{tokenHash}";
            return await eventRepository.GetItemAsync(consumedEventId) is null
                ? NewsletterUnsubscribeResult.Invalid
                : NewsletterUnsubscribeResult.Consumed;
        }
        var eventId = $"unsubscribe:{channelId}:{tokenHash}";
        if (await eventRepository.GetItemAsync(eventId) is null)
            await eventRepository.AddItemAsync(new NewsletterEvent(channelId, string.Empty, NewsletterEventType.Unsubscribe, now) { Id = eventId });
        return NewsletterUnsubscribeResult.Valid;
    }

    public Task<Newsletter> CreateAsync(Newsletter newsletter, CancellationToken cancellationToken = default) => newsletterRepository.AddItemAsync(newsletter);

    public async Task<Newsletter?> TransitionAsync(string channelId, string id, NewsletterState target, CancellationToken cancellationToken = default)
    {
        var newsletter = (await newsletterRepository.GetItemsAsync(item => item.Id == id && item.ChannelId == channelId)).FirstOrDefault();
        if (newsletter is null || !IsValidTransition(newsletter.State, target)) return null;
        var updated = newsletter with { State = target, ApprovedAt = target == NewsletterState.Approved ? DateTime.UtcNow : newsletter.ApprovedAt };
        await newsletterRepository.UpdateItemAsync(updated);
        return updated;
    }

    private static bool IsValidTransition(NewsletterState current, NewsletterState target) => current switch
    {
        NewsletterState.Draft => target is NewsletterState.Editing or NewsletterState.ReadyForPreview or NewsletterState.Cancelled,
        NewsletterState.Editing => target is NewsletterState.ReadyForPreview or NewsletterState.Cancelled,
        NewsletterState.ReadyForPreview => target is NewsletterState.Preview or NewsletterState.Approved,
        NewsletterState.Preview => target is NewsletterState.Editing or NewsletterState.Approved,
        NewsletterState.Approved => target is NewsletterState.Sending or NewsletterState.Cancelled,
        NewsletterState.Sending => target is NewsletterState.Sent or NewsletterState.Failed,
        _ => false
    };

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string BuildPublicLink(string action, string channelId, string token, IConfiguration configuration)
    {
        var baseUrl = configuration["Newsletter:PublicBaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("Newsletter:PublicBaseUrl must be configured.");
        return $"{baseUrl}/newsletter?channelId={Uri.EscapeDataString(channelId)}&action={action}&token={Uri.EscapeDataString(token)}";
    }

    private static string Encrypt(string value, string configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey))
            throw new InvalidOperationException("Newsletter:EncryptionKey must be configured before storing subscriber data.");
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plaintext = Encoding.UTF8.GetBytes(value);
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        return Convert.ToBase64String([.. aes.IV, .. ciphertext]);
    }
}

public enum NewsletterUnsubscribeResult { Valid, Expired, Consumed, Invalid }
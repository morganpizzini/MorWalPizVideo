using System.Net.Mail;
using Hangfire;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class NewsletterTransientException(string message, Exception? inner = null) : Exception(message, inner);

public interface INewsletterDispatchService
{
    Task<bool> QueueAsync(string channelId, string newsletterId, CancellationToken cancellationToken = default);
    Task<bool> QueueScheduledAsync(string channelId, string newsletterId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default);
    Task ProcessScheduledAsync(string channelId, string newsletterId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default);
    Task ReconcileScheduledAsync(CancellationToken cancellationToken = default);
    Task ProcessBatchAsync(string channelId, string newsletterId, CancellationToken cancellationToken = default);
}

public sealed class NewsletterDispatchService(
    INewsletterRepository newsletterRepository,
    INewsletterUserRepository userRepository,
    INewsletterRecipientRepository recipientRepository,
    INewsletterRecipientDispatchRepository dispatchRepository,
    INewsletterEventRepository eventRepository,
    INewsletterEmailService emailService,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<NewsletterDispatchService> logger) : INewsletterDispatchService
{
    public async Task<bool> QueueAsync(string channelId, string newsletterId, CancellationToken cancellationToken = default)
    {
        var newsletter = (await newsletterRepository.GetItemsAsync(item => item.Id == newsletterId && item.ChannelId == channelId)).FirstOrDefault();
        var backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
        if (newsletter is null || newsletter.State != NewsletterState.Approved || backgroundJobClient is null)
            return false;

        var claimed = await newsletterRepository.ClaimForSendingAsync(channelId, newsletterId, NewsletterState.Approved, DateTime.UtcNow, cancellationToken: cancellationToken);
        if (claimed is null) return false;
        await ProvisionRecipientsAsync(claimed, cancellationToken);
        backgroundJobClient.Enqueue<NewsletterDispatchService>(service => service.ProcessBatchAsync(channelId, newsletterId, CancellationToken.None));
        logger.LogInformation("Newsletter {NewsletterId} queued for channel {ChannelId}", newsletterId, channelId);
        return true;
    }

    public async Task<bool> QueueScheduledAsync(string channelId, string newsletterId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default)
    {
        var backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
        if (backgroundJobClient is null) return false;
        var delay = scheduledAtUtc - DateTime.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        backgroundJobClient.Schedule<NewsletterDispatchService>(service => service.ProcessScheduledAsync(channelId, newsletterId, scheduledAtUtc, CancellationToken.None), delay);
        return true;
    }

    public async Task ProcessScheduledAsync(string channelId, string newsletterId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default)
    {
        if (scheduledAtUtc > DateTime.UtcNow) return;
        var claimed = await newsletterRepository.ClaimForSendingAsync(channelId, newsletterId, NewsletterState.Scheduled, DateTime.UtcNow, scheduledAtUtc, cancellationToken);
        if (claimed is null) return;
        await ProvisionRecipientsAsync(claimed, cancellationToken);
        await ProcessBatchAsync(channelId, newsletterId, cancellationToken);
    }

    public async Task ReconcileScheduledAsync(CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(configuration.GetValue("Newsletter:ReconciliationBatchSize", 100), 1, 1000);
        var due = await newsletterRepository.GetDueScheduledAsync(DateTime.UtcNow, limit, cancellationToken);
        foreach (var newsletter in due)
            await ProcessScheduledAsync(newsletter.ChannelId, newsletter.Id, newsletter.ScheduledAtUtc!.Value, cancellationToken);
    }

    private async Task ProvisionRecipientsAsync(Newsletter newsletter, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetItemsAsync(item => item.ChannelId == newsletter.ChannelId && item.Status == NewsletterUserStatus.Subscribed);
        foreach (var user in users)
        {
            await dispatchRepository.EnsurePendingAsync(new NewsletterRecipient(
                newsletter.ChannelId, newsletter.Id, user.Id, user.Language, NewsletterRecipientStatus.Pending)
            { IdempotencyKey = $"{newsletter.ChannelId}:{newsletter.Id}:{user.Id}" }, cancellationToken);
        }
    }

    [AutomaticRetry(Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ProcessBatchAsync(string channelId, string newsletterId, CancellationToken cancellationToken = default)
    {
        var newsletter = (await newsletterRepository.GetItemsAsync(item => item.Id == newsletterId && item.ChannelId == channelId)).FirstOrDefault();
        if (newsletter is null || newsletter.State is NewsletterState.Sent or NewsletterState.Cancelled)
            return;

        var backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
        var batchSize = Math.Clamp(configuration.GetValue("Newsletter:BatchSize", 50), 1, 500);
        var lease = TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("Newsletter:SendingLeaseMinutes", 30), 1, 240));
        var recipients = await dispatchRepository.ClaimBatchAsync(channelId, newsletterId, batchSize, DateTime.UtcNow, lease, cancellationToken);
        var hasTransientFailure = false;
        foreach (var recipient in recipients)
        {
            var user = await userRepository.GetItemAsync(recipient.NewsletterUserId);
            if (user is null || user.ChannelId != channelId || user.Status != NewsletterUserStatus.Subscribed)
            {
                await dispatchRepository.MarkFailedAsync(recipient.Id, "Subscriber is no longer subscribed.", false, DateTime.UtcNow, cancellationToken);
                continue;
            }

            try
            {
                var email = NewsletterCrypto.Decrypt(user.EmailCiphertext, configuration["Newsletter:EncryptionKey"] ?? string.Empty);
                var subject = recipient.Language == "IT" ? newsletter.SubjectIt : newsletter.SubjectEng;
                var unsubscribeLink = user.UnsubscribeTokenCiphertext is null ? null : $"{configuration["Newsletter:PublicBaseUrl"]?.TrimEnd('/')}/newsletter?channelId={Uri.EscapeDataString(channelId)}&action=unsubscribe&token={Uri.EscapeDataString(NewsletterCrypto.Decrypt(user.UnsubscribeTokenCiphertext, configuration["Newsletter:EncryptionKey"] ?? string.Empty))}";
                var sendResult = await emailService.SendAsync(email, subject, RenderHtml(newsletter, unsubscribeLink), recipient.IdempotencyKey, cancellationToken);
                await dispatchRepository.MarkSentAsync(recipient.Id, sendResult.ProviderMessageId, DateTime.UtcNow, cancellationToken);
                if (await eventRepository.GetItemAsync(recipient.IdempotencyKey) is null)
                    await eventRepository.AddItemAsync(new NewsletterEvent(channelId, newsletterId, NewsletterEventType.Send, DateTime.UtcNow, ProviderMessageId: sendResult.ProviderMessageId) { Id = recipient.IdempotencyKey });
            }
            catch (Exception exception) when (IsPermanent(exception))
            {
                await dispatchRepository.MarkSuppressedAsync(recipient.Id, exception.Message, DateTime.UtcNow, cancellationToken);
                await userRepository.UpdateItemAsync(user with { Status = NewsletterUserStatus.Suppressed });
            }
            catch (Exception exception)
            {
                await dispatchRepository.MarkFailedAsync(recipient.Id, exception.Message, true, DateTime.UtcNow, cancellationToken);
                hasTransientFailure = true;
                logger.LogWarning(exception, "Transient newsletter delivery failure for recipient {RecipientId}", recipient.Id);
            }
        }

        if (hasTransientFailure)
            throw new NewsletterTransientException($"Transient delivery failure in newsletter {newsletterId}.");

        var outstanding = (await recipientRepository.GetItemsAsync(item => item.ChannelId == channelId && item.NewsletterId == newsletterId))
            .Any(item => item.Status is NewsletterRecipientStatus.Pending or NewsletterRecipientStatus.Sending);
        if (outstanding && backgroundJobClient is not null)
            backgroundJobClient.Enqueue<NewsletterDispatchService>(service => service.ProcessBatchAsync(channelId, newsletterId, CancellationToken.None));
        else if (!outstanding)
            await newsletterRepository.UpdateItemAsync(newsletter with { State = NewsletterState.Sent });
    }

    private static bool IsPermanent(Exception exception) => exception switch
    {
        FormatException => true,
        ArgumentException => true,
        SmtpException smtp when (int)smtp.StatusCode is >= 500 and < 600 => true,
        _ => false
    };

    private string RenderHtml(Newsletter newsletter, string? unsubscribeLink) => string.Join("\n", newsletter.Sections.Select(section => section.Type switch
    {
        "heading" => $"<h2>{System.Net.WebUtility.HtmlEncode(section.Title)}</h2>",
        "text" => $"<p>{System.Net.WebUtility.HtmlEncode(section.Body)}</p>",
        "image" when !string.IsNullOrWhiteSpace(section.ImageUrl) => $"<img src=\"{System.Net.WebUtility.HtmlEncode(section.ImageUrl)}\" alt=\"\" />",
        "link" when !string.IsNullOrWhiteSpace(section.ShortLinkCode) => $"<a href=\"{System.Net.WebUtility.HtmlEncode(BuildShortLinkUrl(newsletter, section.ShortLinkCode))}\">{System.Net.WebUtility.HtmlEncode(section.Title ?? section.ShortLinkCode)}</a>",
        _ => string.Empty
    })) + (unsubscribeLink is null ? string.Empty : $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(unsubscribeLink)}\">Unsubscribe</a></p>");

    private string BuildShortLinkUrl(Newsletter newsletter, string code)
    {
        var baseUrl = configuration["Newsletter:ShortLinkBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Newsletter:ShortLinkBaseUrl must be configured before sending links.");

        return $"{baseUrl}/{Uri.EscapeDataString(code.Trim())}?newsletterId={Uri.EscapeDataString(newsletter.Id)}&channelId={Uri.EscapeDataString(newsletter.ChannelId)}";
    }
}

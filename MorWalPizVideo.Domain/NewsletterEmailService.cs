using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MorWalPizVideo.Domain;

public sealed class NewsletterSmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}

public interface INewsletterEmailService
{
    Task<NewsletterSendResult> SendAsync(string recipient, string subject, string htmlBody, string providerKey, CancellationToken cancellationToken = default);
}

public sealed record NewsletterSendResult(string? ProviderMessageId);

public sealed class SmtpNewsletterEmailService(IOptions<NewsletterSmtpOptions> options, ILogger<SmtpNewsletterEmailService> logger) : INewsletterEmailService
{
    public async Task<NewsletterSendResult> SendAsync(string recipient, string subject, string htmlBody, string providerKey, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl, Credentials = new NetworkCredential(settings.Username, settings.Password) };
        using var message = new MailMessage(settings.From, recipient, subject, htmlBody) { IsBodyHtml = true };
        var providerMessageId = $"<{providerKey}@newsletter.local>";
        message.Headers.Add("Message-Id", providerMessageId);
        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Newsletter email sent to recipient {RecipientHash} with provider key {ProviderKey}", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(recipient))), providerKey);
        return new NewsletterSendResult(providerMessageId);
    }
}

public sealed class SmtpMockService : INewsletterEmailService
{
    private readonly object sync = new();
    public List<(string Recipient, string Subject)> Sent { get; } = [];
    public List<(string Recipient, string Subject, string HtmlBody, string ProviderKey)> Messages { get; } = [];

    public void Clear()
    {
        lock (sync) { Sent.Clear(); Messages.Clear(); }
    }

    public Task<NewsletterSendResult> SendAsync(string recipient, string subject, string htmlBody, string providerKey, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            if (Messages.Any(message => message.ProviderKey == providerKey)) return Task.FromResult(new NewsletterSendResult($"<{providerKey}@newsletter.local>"));
            Sent.Add((recipient, subject));
            Messages.Add((recipient, subject, htmlBody, providerKey));
        }
        return Task.FromResult(new NewsletterSendResult($"<{providerKey}@newsletter.local>"));
    }
}
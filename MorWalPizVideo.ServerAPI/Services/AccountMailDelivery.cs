using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.ServerAPI.Services;

public sealed class AccountMailOptions
{
    public NewsletterSmtpOptions Smtp { get; set; } = new();
    public string WorkshopOrigin { get; set; } = string.Empty;
    public string PublicSiteOrigin { get; set; } = string.Empty;

    public bool HasValidOrigins =>
        WorkshopOrigin == "https://workshop.morwalpiz.com" && PublicSiteOrigin == "https://morwalpiz.com";
    public bool IsConfigured => HasValidOrigins && !string.IsNullOrWhiteSpace(Smtp.Host) &&
        Smtp.Port is > 0 and <= 65535 && Smtp.EnableSsl &&
        !string.IsNullOrWhiteSpace(Smtp.Username) && !string.IsNullOrWhiteSpace(Smtp.Password) &&
        MailAddress.TryCreate(Smtp.From, out _);

    public string GetOrigin(string site) => site switch
    {
        "workshop" => WorkshopOrigin,
        "public-site" => PublicSiteOrigin,
        _ => throw new ArgumentException("Unknown account site.", nameof(site))
    };
}

public sealed record AccountMailWork(string Email, string Site, PublicAccountMailPurpose Purpose, bool Automatic = false);
public sealed record AccountMailMessage(string Recipient, PublicAccountMailPurpose Purpose, string Link);

public interface IAccountMailSender
{
    bool IsConfigured { get; }
    Task SendAsync(AccountMailMessage message, CancellationToken cancellationToken);
}

public sealed class AccountSmtpSender(IOptions<AccountMailOptions> options) : IAccountMailSender
{
    public bool IsConfigured => options.Value.IsConfigured;
    public async Task SendAsync(AccountMailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value.Smtp;
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.Username, settings.Password)
        };
        var confirmation = message.Purpose == PublicAccountMailPurpose.EmailConfirmation;
        using var mail = new MailMessage(settings.From, message.Recipient,
            confirmation ? "MorWalPiz - Confirm your email / Conferma la tua email" : "MorWalPiz - Reset your password / Reimposta la password",
            confirmation
                ? $"Confirm your email address. / Conferma il tuo indirizzo email.\n\n{message.Link}\n\nThis link expires in 24 hours. / Il link scade tra 24 ore."
                : $"Choose a new password. / Scegli una nuova password.\n\n{message.Link}\n\nThis link expires in 30 minutes. If you did not request it, ignore this message. / Il link scade tra 30 minuti. Se non lo hai richiesto, ignora questo messaggio.");
        await client.SendMailAsync(mail, cancellationToken);
    }
}

public sealed class AccountMailMockSender(IOptions<AccountMailOptions> options) : IAccountMailSender
{
    private readonly ConcurrentDictionary<(string, PublicAccountMailPurpose), TaskCompletionSource<AccountMailMessage>> messages = new();
    public bool IsConfigured => options.Value.HasValidOrigins;
    public Task<AccountMailMessage> WaitForAsync(string recipient, PublicAccountMailPurpose purpose, CancellationToken cancellationToken) =>
        messages.GetOrAdd((recipient.ToUpperInvariant(), purpose), _ => new(TaskCreationOptions.RunContinuationsAsynchronously)).Task.WaitAsync(cancellationToken);
    public Task SendAsync(AccountMailMessage message, CancellationToken cancellationToken)
    {
        messages.GetOrAdd((message.Recipient.ToUpperInvariant(), message.Purpose), _ => new(TaskCreationOptions.RunContinuationsAsynchronously))
            .TrySetResult(message);
        return Task.CompletedTask;
    }
}

public sealed class AccountMailQueue(IAccountMailSender sender)
{
    private readonly Channel<AccountMailWork> channel = Channel.CreateBounded<AccountMailWork>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    public bool IsAvailable => sender.IsConfigured;
    public bool TryEnqueue(AccountMailWork work) => IsAvailable && channel.Writer.TryWrite(work);
    public IAsyncEnumerable<AccountMailWork> ReadAllAsync(CancellationToken cancellationToken) => channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class AccountMailWorker(IServiceScopeFactory scopes, AccountMailQueue queue,
    IAccountMailSender sender, IOptions<AccountMailOptions> options, ILogger<AccountMailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!queue.IsAvailable)
            logger.LogError("Account mail is unavailable. Configure PublicAccount:Mail through protected configuration.");
        await foreach (var work in queue.ReadAllAsync(stoppingToken))
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                using var scope = scopes.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PublicAccountMailService>();
                var link = await service.IssueAsync(work.Email, work.Purpose, work.Automatic, timeout.Token);
                if (link is null) continue;
                var path = work.Purpose == PublicAccountMailPurpose.PasswordReset ? "/account/reset-password" : "/account/confirm-email";
                var url = $"{options.Value.GetOrigin(work.Site)}{path}#accountId={link.Account.Id}&token={link.Token}";
                await sender.SendAsync(new(link.Account.Email, work.Purpose, url), timeout.Token);
                logger.LogInformation("Account mail accepted by SMTP for {Purpose}", work.Purpose);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Account mail timed out for {Purpose}", work.Purpose);
            }
            catch (Exception exception) when (exception is SmtpException or MongoException or InvalidOperationException or IOException)
            {
                // Provider exception text can contain mailbox addresses or credentials.
                logger.LogError("Account mail failed for {Purpose} with {FailureType}", work.Purpose, exception.GetType().Name);
            }
        }
    }
}

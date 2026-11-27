using System.Net;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:ServerAPI")]
public sealed class PublicAccountMailEndpointTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;
    public PublicAccountMailEndpointTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("workshop", "https://workshop.morwalpiz.com")]
    [InlineData("public-site", "https://morwalpiz.com")]
    public async Task Registration_requires_confirmation_and_mail_links_return_to_the_requesting_site(string site, string origin)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = (await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf"))!;
        var registration = NewRegistration(site);
        var response = await Send(client, "/api/v1/accounts", registration, csrf.Token);
        var pending = (await response.Content.ReadFromJsonAsync<PublicAccountSessionContract>())!;
        Assert.False(pending.Authenticated);
        Assert.True(pending.EmailConfirmationRequired);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(cookie => cookie.StartsWith("public_account_session=")));
        var blocked = await Send(client, "/api/v1/account/sessions",
            new PublicAccountLoginRequest { Identifier = registration.Email, Password = registration.Password, Site = site }, csrf.Token);
        Assert.True((await blocked.Content.ReadFromJsonAsync<PublicAccountSessionContract>())!.EmailConfirmationRequired);
        Assert.False((await client.GetFromJsonAsync<PublicAccountSessionContract>("/api/v1/account/session"))!.Authenticated);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var confirmation = await factory.Services.GetRequiredService<AccountMailMockSender>()
            .WaitForAsync(registration.Email, PublicAccountMailPurpose.EmailConfirmation, timeout.Token);
        Assert.StartsWith(origin + "/account/confirm-email#", confirmation.Link);
        var token = PublicAccountTestFlow.TokenFromLink(confirmation.Link);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, "/api/v1/account/email-confirmations", token, csrf.Token)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/account/email-confirmations", token, csrf.Token)).StatusCode);
        var login = await Send(client, "/api/v1/account/sessions",
            new PublicAccountLoginRequest { Identifier = registration.Email, Password = registration.Password, Site = site }, csrf.Token);
        var session = (await login.Content.ReadFromJsonAsync<PublicAccountSessionContract>())!;
        Assert.True(session.Authenticated);
        Assert.True(session.Account!.EmailConfirmed);

        var reset = await Send(client, "/api/v1/account/password-recovery-requests", new PublicAccountMailRequest
        {
            Email = registration.Email, Site = site
        }, csrf.Token);
        Assert.Equal(HttpStatusCode.Accepted, reset.StatusCode);
        Assert.Contains("no-store", reset.Headers.CacheControl!.ToString());
        var resetMail = await factory.Services.GetRequiredService<AccountMailMockSender>()
            .WaitForAsync(registration.Email, PublicAccountMailPurpose.PasswordReset, timeout.Token);
        Assert.StartsWith(origin + "/account/reset-password#", resetMail.Link);
        var resetToken = PublicAccountTestFlow.TokenFromLink(resetMail.Link);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/account/email-confirmations", resetToken, csrf.Token)).StatusCode);
        var newPassword = "A-new-password-456";
        var resetRequest = new PublicAccountPasswordResetRequest { AccountId = resetToken.AccountId, Token = resetToken.Token, Password = newPassword };
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, "/api/v1/account/password-resets", resetRequest, csrf.Token)).StatusCode);
        Assert.False((await client.GetFromJsonAsync<PublicAccountSessionContract>("/api/v1/account/session"))!.Authenticated);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/account/password-resets", resetRequest, csrf.Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, "/api/v1/account/sessions",
            new PublicAccountLoginRequest { Identifier = registration.Email, Password = registration.Password }, csrf.Token)).StatusCode);
        Assert.True((await (await Send(client, "/api/v1/account/sessions",
            new PublicAccountLoginRequest { Identifier = registration.Email, Password = newPassword }, csrf.Token))
            .Content.ReadFromJsonAsync<PublicAccountSessionContract>())!.Authenticated);
    }

    [Fact]
    public async Task Legacy_login_is_permitted_and_requests_confirmation_without_marking_the_email_verified()
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPublicAccountService>();
        var registration = NewRegistration("workshop");
        await service.RegisterAsync(new(registration.Username, registration.Email, registration.Password, "", true, true));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = (await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf"))!;
        var response = await Send(client, "/api/v1/account/sessions",
            new PublicAccountLoginRequest { Identifier = registration.Email, Password = registration.Password, Site = "public-site" }, csrf.Token);
        var session = (await response.Content.ReadFromJsonAsync<PublicAccountSessionContract>())!;
        Assert.True(session.Authenticated);
        Assert.False(session.Account!.EmailConfirmed);
        Assert.Equal("requested", session.ConfirmationEmailStatus);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var mail = await factory.Services.GetRequiredService<AccountMailMockSender>()
            .WaitForAsync(registration.Email, PublicAccountMailPurpose.EmailConfirmation, timeout.Token);
        Assert.StartsWith("https://morwalpiz.com/account/confirm-email#", mail.Link);
    }

    [Fact]
    public async Task Mail_requests_require_csrf_reject_arbitrary_sites_and_conceal_unknown_email()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var request = new PublicAccountMailRequest { Email = "missing@example.test", Site = "workshop" };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/account/password-recovery-requests", request)).StatusCode);
        var csrf = (await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf"))!;
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/account/password-recovery-requests",
            request with { Site = "https://evil.example" }, csrf.Token)).StatusCode);
        var response = await Send(client, "/api/v1/account/password-recovery-requests", request, csrf.Token);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<PublicAccountMailReceipt>())!.Accepted);
    }

    [Fact]
    public async Task Mail_request_throttling_is_independent_of_the_optional_login_limiter()
    {
        await using var isolated = new ServerApiWebApplicationFactory();
        using var client = isolated.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = (await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf"))!;
        var request = new PublicAccountMailRequest { Email = "missing@example.test", Site = "workshop" };
        for (var index = 0; index < 10; index++)
            Assert.Equal(HttpStatusCode.Accepted, (await Send(client, "/api/v1/account/password-recovery-requests", request, csrf.Token)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Send(client, "/api/v1/account/password-recovery-requests", request, csrf.Token)).StatusCode);
    }

    private static PublicAccountRegistrationRequest NewRegistration(string site) => new()
    {
        Site = site, Username = $"mail{Guid.NewGuid():N}"[..16], Email = $"{Guid.NewGuid():N}@example.test",
        Password = "A-strong-password-123", TermsAccepted = true, PrivacyNoticeAcknowledged = true
    };
    private static Task<HttpResponseMessage> Send(HttpClient client, string path, object payload, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return client.SendAsync(request);
    }
}

public sealed class PublicAccountMailStateTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 26, 22, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Theory]
    [InlineData(PublicAccountMailPurpose.PasswordReset)]
    [InlineData(PublicAccountMailPurpose.EmailConfirmation)]
    public async Task Tokens_expire_are_superseded_and_are_consumed_once_under_concurrency(PublicAccountMailPurpose purpose)
    {
        var repository = new PublicAccountMockRepository(new PrimaryScenario());
        var accounts = new PublicAccountService(repository);
        var account = (await accounts.RegisterPendingAsync(new("mailuser", "mail@example.test", "original-password", "", true, true)))!;
        var clock = new Clock();
        var service = new PublicAccountMailService(repository, clock);
        var old = (await service.IssueAsync(account.Email, purpose))!;
        var current = (await service.IssueAsync(account.Email, purpose))!;
        Assert.False(await service.ConsumeAsync(account.Id, old.Token, purpose, "replacement-password"));
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            service.ConsumeAsync(account.Id, current.Token, purpose, "replacement-password")));
        Assert.Equal(1, results.Count(result => result));
        var expired = (await service.IssueAsync(account.Email,
            purpose == PublicAccountMailPurpose.EmailConfirmation ? PublicAccountMailPurpose.PasswordReset : purpose))!;
        clock.Now = clock.Now.AddMinutes(30);
        Assert.False(await service.ConsumeAsync(account.Id, expired.Token, PublicAccountMailPurpose.PasswordReset, "replacement-password"));
        if (purpose == PublicAccountMailPurpose.PasswordReset)
        {
            Assert.Null(await accounts.LoginAsync(new(account.Email, "replacement-password")));
            Assert.False((await repository.GetAccountAsync(account.Id))!.EmailConfirmed);
        }
    }

    [Fact]
    public async Task Profile_email_changes_invalidate_both_tokens_and_do_not_erase_other_account_fields()
    {
        var repository = new PublicAccountMockRepository(new PrimaryScenario());
        var accounts = new PublicAccountService(repository);
        var account = (await accounts.RegisterPendingAsync(new("emailchange", "before@example.test", "original-password", "", true, true)))!;
        var service = new PublicAccountMailService(repository, new Clock());
        var confirmation = (await service.IssueAsync(account.Email, PublicAccountMailPurpose.EmailConfirmation))!;
        var reset = (await service.IssueAsync(account.Email, PublicAccountMailPurpose.PasswordReset))!;
        await repository.UpdateProfileAsync(account.Id, "after@example.test", "AFTER@EXAMPLE.TEST", "Changed");
        Assert.False(await service.ConsumeAsync(account.Id, confirmation.Token, PublicAccountMailPurpose.EmailConfirmation));
        Assert.False(await service.ConsumeAsync(account.Id, reset.Token, PublicAccountMailPurpose.PasswordReset, "replacement-password"));
        var updated = (await repository.GetAccountAsync(account.Id))!;
        Assert.Equal(account.PasswordHash, updated.PasswordHash);
        Assert.True(updated.EmailConfirmationRequired);
        Assert.Equal("Changed", updated.DisplayName);
        Assert.False(updated.EmailConfirmed);
    }

    [Fact]
    public async Task Automatic_confirmation_is_suppressed_until_expiry_and_inactive_accounts_cannot_redeem()
    {
        var repository = new PublicAccountMockRepository(new PrimaryScenario());
        var accounts = new PublicAccountService(repository);
        var account = (await accounts.RegisterPendingAsync(new("expiryuser", "expiry@example.test", "original-password", "", true, true)))!;
        var clock = new Clock();
        var service = new PublicAccountMailService(repository, clock);
        var confirmation = (await service.IssueAsync(account.Email, PublicAccountMailPurpose.EmailConfirmation))!;
        Assert.Null(await service.IssueAsync(account.Email, PublicAccountMailPurpose.EmailConfirmation, true));
        clock.Now = clock.Now.AddHours(24);
        Assert.False(await service.ConsumeAsync(account.Id, confirmation.Token, PublicAccountMailPurpose.EmailConfirmation));
        var replacement = (await service.IssueAsync(account.Email, PublicAccountMailPurpose.EmailConfirmation, true))!;
        await repository.UpdateAccountAsync((await repository.GetAccountAsync(account.Id))! with { IsActive = false });
        Assert.False(await service.ConsumeAsync(account.Id, replacement.Token, PublicAccountMailPurpose.EmailConfirmation));
        Assert.Null(await service.IssueAsync(account.Email, PublicAccountMailPurpose.PasswordReset));
    }

    private sealed class FailingOnceSender : IAccountMailSender
    {
        public bool IsConfigured => true;
        public TaskCompletionSource Delivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int attempts;
        public Task SendAsync(AccountMailMessage message, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref attempts) == 1)
                throw new System.Net.Mail.SmtpException("private-mailbox@example.test credential-sentinel");
            Delivered.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class MailLogger : ILogger<AccountMailWorker>
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Enqueue(formatter(state, exception));
    }

    [Fact]
    public async Task Worker_surfaces_smtp_failure_without_sensitive_details_and_continues_processing()
    {
        var repository = new PublicAccountMockRepository(new PrimaryScenario());
        var accounts = new PublicAccountService(repository);
        var account = (await accounts.RegisterPendingAsync(new("workeruser", "private-mailbox@example.test", "original-password", "", true, true)))!;
        using var services = new ServiceCollection().AddSingleton<IPublicAccountRepository>(repository)
            .AddSingleton<TimeProvider>(new Clock()).AddScoped<PublicAccountMailService>().BuildServiceProvider();
        var sender = new FailingOnceSender();
        var queue = new AccountMailQueue(sender);
        var logger = new MailLogger();
        using var worker = new AccountMailWorker(services.GetRequiredService<IServiceScopeFactory>(), queue, sender,
            Options.Create(new AccountMailOptions { WorkshopOrigin = "https://workshop.morwalpiz.com", PublicSiteOrigin = "https://morwalpiz.com" }), logger);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(queue.TryEnqueue(new(account.Email, "workshop", PublicAccountMailPurpose.EmailConfirmation)));
            Assert.True(queue.TryEnqueue(new(account.Email, "workshop", PublicAccountMailPurpose.PasswordReset)));
            await sender.Delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var logs = string.Join("\n", logger.Messages);
            Assert.Contains("SmtpException", logs);
            Assert.DoesNotContain(account.Email, logs);
            Assert.DoesNotContain("credential-sentinel", logs);
        }
        finally { await worker.StopAsync(CancellationToken.None); }
    }

    [Fact]
    public void Legacy_bson_has_no_confirmation_gate_and_queue_admission_fails_explicitly_when_unavailable_or_full()
    {
        var legacy = BsonSerializer.Deserialize<PublicAccount>("{ 'username': 'legacy' }");
        Assert.False(legacy.EmailConfirmationRequired);
        Assert.False(legacy.EmailConfirmed);
        var unavailable = new AccountMailQueue(new AccountSmtpSender(Options.Create(new AccountMailOptions())));
        Assert.False(unavailable.TryEnqueue(new("missing@example.test", "workshop", PublicAccountMailPurpose.PasswordReset)));
        var options = Options.Create(new AccountMailOptions
        {
            WorkshopOrigin = "https://workshop.morwalpiz.com", PublicSiteOrigin = "https://morwalpiz.com"
        });
        var queue = new AccountMailQueue(new AccountMailMockSender(options));
        for (var index = 0; index < 100; index++)
            Assert.True(queue.TryEnqueue(new("missing@example.test", "workshop", PublicAccountMailPurpose.PasswordReset)));
        Assert.False(queue.TryEnqueue(new("missing@example.test", "workshop", PublicAccountMailPurpose.PasswordReset)));
    }
}

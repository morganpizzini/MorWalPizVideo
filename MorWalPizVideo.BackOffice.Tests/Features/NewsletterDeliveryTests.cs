using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class NewsletterDeliveryTests
{
    [Fact]
    public async Task Subscribe_sends_confirmation_link_and_does_not_persist_raw_token()
    {
        var users = new TestUserRepository();
        var email = new SmtpMockService();
        var service = new NewsletterService(new EmptyNewsletterRepository(), users, email, new EmptyEventRepository(), Configuration());

        var token = await service.SubscribeAsync("channel-a", "Person@example.com", "IT");

        token.Should().NotBeNullOrWhiteSpace();
        email.Messages.Should().ContainSingle();
        email.Messages[0].HtmlBody.Should().Contain("action=confirm");
        users.Item!.ConfirmationTokenHash.Should().NotBe(token);
    }

    [Fact]
    public async Task Mock_provider_deduplicates_same_provider_key_after_retry()
    {
        var email = new SmtpMockService();
        await email.SendAsync("person@example.com", "Subject", "<p>body</p>", "channel:newsletter:user");
        await email.SendAsync("person@example.com", "Subject", "<p>body</p>", "channel:newsletter:user");

        email.Messages.Should().ContainSingle();
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Newsletter:EncryptionKey"] = "test-only-newsletter-key",
        ["Newsletter:PublicBaseUrl"] = "https://example.test"
    }).Build();

    private sealed class TestUserRepository : INewsletterUserRepository
    {
        public NewsletterUser? Item { get; private set; }
        public Task<NewsletterUser> AddItemAsync(NewsletterUser item) { Item = item with { Id = "user-1" }; return Task.FromResult(Item); }
        public Task DeleteItemAsync(string id) => Task.CompletedTask;
        public Task<NewsletterUser> GetItemAsync(string id) => Task.FromResult(Item!);
        public Task<IList<NewsletterUser>> GetItemsAsync() => Task.FromResult<IList<NewsletterUser>>(Item is null ? [] : [Item]);
        public Task<IList<NewsletterUser>> GetItemsAsync(System.Linq.Expressions.Expression<Func<NewsletterUser, bool>> predicate) => Task.FromResult<IList<NewsletterUser>>(GetItemsAsync().Result.AsQueryable().Where(predicate).ToList());
        public Task UpdateItemAsync(NewsletterUser item) { Item = item; return Task.CompletedTask; }
        public Task<NewsletterUser?> ConsumeConfirmationAsync(string channelId, string tokenHash, DateTime now, CancellationToken cancellationToken = default) => Task.FromResult<NewsletterUser?>(null);
        public Task<NewsletterUser?> ConsumeUnsubscribeAsync(string channelId, string tokenHash, DateTime now, CancellationToken cancellationToken = default) => Task.FromResult<NewsletterUser?>(null);
    }

    private sealed class EmptyNewsletterRepository : INewsletterRepository
    {
        public Task<Newsletter> AddItemAsync(Newsletter item) => Task.FromResult(item);
        public Task DeleteItemAsync(string id) => Task.CompletedTask;
        public Task<Newsletter> GetItemAsync(string id) => Task.FromResult<Newsletter>(null!);
        public Task<IList<Newsletter>> GetItemsAsync() => Task.FromResult<IList<Newsletter>>([]);
        public Task<IList<Newsletter>> GetItemsAsync(System.Linq.Expressions.Expression<Func<Newsletter, bool>> predicate) => Task.FromResult<IList<Newsletter>>([]);
        public Task UpdateItemAsync(Newsletter item) => Task.CompletedTask;
        public Task<Newsletter?> ClaimForSendingAsync(string channelId, string newsletterId, NewsletterState expectedState, DateTime now, DateTime? expectedScheduledAtUtc = null, CancellationToken cancellationToken = default) => Task.FromResult<Newsletter?>(null);
        public Task<IList<Newsletter>> GetDueScheduledAsync(DateTime now, int limit, CancellationToken cancellationToken = default) => Task.FromResult<IList<Newsletter>>([]);
    }

    private sealed class EmptyEventRepository : INewsletterEventRepository
    {
        public Task<NewsletterEvent> AddItemAsync(NewsletterEvent item) => Task.FromResult(item);
        public Task DeleteItemAsync(string id) => Task.CompletedTask;
        public Task<NewsletterEvent> GetItemAsync(string id) => Task.FromResult<NewsletterEvent>(null!);
        public Task<IList<NewsletterEvent>> GetItemsAsync() => Task.FromResult<IList<NewsletterEvent>>([]);
        public Task<IList<NewsletterEvent>> GetItemsAsync(System.Linq.Expressions.Expression<Func<NewsletterEvent, bool>> predicate) => Task.FromResult<IList<NewsletterEvent>>([]);
        public Task UpdateItemAsync(NewsletterEvent item) => Task.CompletedTask;
        public Task RecordClickAsync(string channelId, string newsletterId, string shortLinkCode, DateTime occurredAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

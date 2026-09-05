using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class NewsletterUnsubscribeTests
{
    [Fact]
    public async Task Valid_token_is_consumed_once_and_second_attempt_is_not_success()
    {
        var token = "valid-token";
        var users = new UserRepository(new NewsletterUser("channel-a", Hash("person@example.com"), "cipher", "IT")
        {
            Id = "user-1",
            Status = NewsletterUserStatus.Subscribed,
            UnsubscribeTokenHash = Hash(token),
            UnsubscribeExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        var events = new EventRepository();
        var service = CreateService(users, events);

        (await service.UnsubscribeAsync("channel-a", token)).Should().Be(NewsletterUnsubscribeResult.Valid);
        (await service.UnsubscribeAsync("channel-a", token)).Should().Be(NewsletterUnsubscribeResult.Consumed);
        users.Item!.Status.Should().Be(NewsletterUserStatus.Unsubscribed);
        events.Items.Should().ContainSingle(item => item.Type == NewsletterEventType.Unsubscribe);
    }

    [Fact]
    public async Task Expired_and_unknown_tokens_are_not_reported_as_success()
    {
        var users = new UserRepository(new NewsletterUser("channel-a", "hash", "cipher", "IT")
        {
            UnsubscribeTokenHash = Hash("expired-token"),
            UnsubscribeExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        });
        var service = CreateService(users, new EventRepository());

        (await service.UnsubscribeAsync("channel-a", "expired-token")).Should().Be(NewsletterUnsubscribeResult.Expired);
        (await service.UnsubscribeAsync("channel-a", "unknown-token")).Should().Be(NewsletterUnsubscribeResult.Invalid);
    }

    private static NewsletterService CreateService(UserRepository users, EventRepository events) =>
        new(new EmptyNewsletterRepository(), users, new SmtpMockService(), events,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build());

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class UserRepository(NewsletterUser item) : INewsletterUserRepository
    {
        public NewsletterUser? Item { get; private set; } = item;
        public Task<NewsletterUser> AddItemAsync(NewsletterUser value) => Task.FromResult(value);
        public Task DeleteItemAsync(string id) => Task.CompletedTask;
        public Task<NewsletterUser> GetItemAsync(string id) => Task.FromResult(Item!);
        public Task<IList<NewsletterUser>> GetItemsAsync() => Task.FromResult<IList<NewsletterUser>>(Item is null ? [] : [Item]);
        public Task<IList<NewsletterUser>> GetItemsAsync(Expression<Func<NewsletterUser, bool>> predicate) => Task.FromResult<IList<NewsletterUser>>(GetItemsAsync().Result.AsQueryable().Where(predicate).ToList());
        public Task UpdateItemAsync(NewsletterUser value) { Item = value; return Task.CompletedTask; }
        public Task<NewsletterUser?> ConsumeConfirmationAsync(string channelId, string tokenHash, DateTime now, CancellationToken cancellationToken = default) => Task.FromResult<NewsletterUser?>(null);
        public Task<NewsletterUser?> ConsumeUnsubscribeAsync(string channelId, string tokenHash, DateTime now, CancellationToken cancellationToken = default)
        {
            if (Item?.ChannelId != channelId || Item.UnsubscribeTokenHash != tokenHash || Item.UnsubscribeExpiresAt <= now)
                return Task.FromResult<NewsletterUser?>(null);
            var previous = Item;
            Item = Item with { UnsubscribeTokenHash = null, UnsubscribeTokenCiphertext = null, UnsubscribeExpiresAt = null, Status = NewsletterUserStatus.Unsubscribed };
            return Task.FromResult<NewsletterUser?>(previous);
        }
    }

    private sealed class EventRepository : INewsletterEventRepository
    {
        public List<NewsletterEvent> Items { get; } = [];
        public Task<NewsletterEvent> AddItemAsync(NewsletterEvent item) { Items.Add(item); return Task.FromResult(item); }
        public Task DeleteItemAsync(string id) => Task.CompletedTask;
        public Task<NewsletterEvent> GetItemAsync(string id) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id)!);
        public Task<IList<NewsletterEvent>> GetItemsAsync() => Task.FromResult<IList<NewsletterEvent>>(Items);
        public Task<IList<NewsletterEvent>> GetItemsAsync(Expression<Func<NewsletterEvent, bool>> predicate) => Task.FromResult<IList<NewsletterEvent>>(Items.AsQueryable().Where(predicate).ToList());
        public Task UpdateItemAsync(NewsletterEvent item) => Task.CompletedTask;
        public Task RecordClickAsync(string channelId, string newsletterId, string shortLinkCode, DateTime occurredAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyNewsletterRepository : INewsletterRepository
    {
        public Task<Newsletter> AddItemAsync(Newsletter item) => Task.FromResult(item);
        public Task DeleteItemAsync(string id) => Task.CompletedTask;
        public Task<Newsletter> GetItemAsync(string id) => Task.FromResult<Newsletter>(null!);
        public Task<IList<Newsletter>> GetItemsAsync() => Task.FromResult<IList<Newsletter>>([]);
        public Task<IList<Newsletter>> GetItemsAsync(Expression<Func<Newsletter, bool>> predicate) => Task.FromResult<IList<Newsletter>>([]);
        public Task UpdateItemAsync(Newsletter item) => Task.CompletedTask;
    }
}
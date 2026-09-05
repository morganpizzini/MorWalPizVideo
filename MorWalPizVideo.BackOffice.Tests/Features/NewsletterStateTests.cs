using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class NewsletterStateTests
{
    [Fact]
    public async Task Invalid_transition_is_rejected_without_persisting()
    {
        var newsletterRepository = new TestNewsletterRepository(new Newsletter("channel-a", "Test", "IT", "EN", "template", 1, [], NewsletterState.Draft));
        var userRepository = new TestNewsletterUserRepository();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Newsletter:EncryptionKey"] = "test-only-newsletter-key"
        }).Build();
        var service = new NewsletterService(newsletterRepository, userRepository, new TestNewsletterEmailService(), new TestNewsletterEventRepository(), configuration);

        var result = await service.TransitionAsync("channel-a", newsletterRepository.Item.Id, NewsletterState.Sent);

        result.Should().BeNull();
        newsletterRepository.Item.State.Should().Be(NewsletterState.Draft);
    }

    private sealed class TestNewsletterRepository(Newsletter item) : MorWalPizVideo.Server.Services.Interfaces.INewsletterRepository
    {
        public Newsletter Item { get; private set; } = item with { Id = "newsletter-1" };
        public Task<Newsletter> AddItemAsync(Newsletter value) => Task.FromResult(value);
        public Task DeleteItemAsync(string id) => Task.CompletedTask;
        public Task<Newsletter> GetItemAsync(string id) => Task.FromResult(Item);
        public Task<IList<Newsletter>> GetItemsAsync() => Task.FromResult<IList<Newsletter>>([Item]);
        public Task<IList<Newsletter>> GetItemsAsync(System.Linq.Expressions.Expression<Func<Newsletter, bool>> predicate) => Task.FromResult<IList<Newsletter>>(new List<Newsletter> { Item }.AsQueryable().Where(predicate).ToList());
        public Task UpdateItemAsync(Newsletter value) { Item = value; return Task.CompletedTask; }
    }

    private sealed class TestNewsletterUserRepository : MorWalPizVideo.Server.Services.Interfaces.INewsletterUserRepository
    {
        public Task<NewsletterUser> AddItemAsync(NewsletterUser item) => Task.FromResult(item);
        public Task DeleteItemAsync(string id) => Task.CompletedTask;
        public Task<NewsletterUser> GetItemAsync(string id) => Task.FromResult<NewsletterUser>(null!);
        public Task<IList<NewsletterUser>> GetItemsAsync() => Task.FromResult<IList<NewsletterUser>>([]);
        public Task<IList<NewsletterUser>> GetItemsAsync(System.Linq.Expressions.Expression<Func<NewsletterUser, bool>> predicate) => Task.FromResult<IList<NewsletterUser>>([]);
        public Task UpdateItemAsync(NewsletterUser item) => Task.CompletedTask;
        public Task<NewsletterUser?> ConsumeConfirmationAsync(string channelId, string tokenHash, DateTime now, CancellationToken cancellationToken = default) => Task.FromResult<NewsletterUser?>(null);
        public Task<NewsletterUser?> ConsumeUnsubscribeAsync(string channelId, string tokenHash, DateTime now, CancellationToken cancellationToken = default) => Task.FromResult<NewsletterUser?>(null);
    }

    private sealed class TestNewsletterEmailService : MorWalPizVideo.Domain.INewsletterEmailService
    {
        public Task<NewsletterSendResult> SendAsync(string recipient, string subject, string htmlBody, string providerKey, CancellationToken cancellationToken = default) => Task.FromResult(new NewsletterSendResult(null));
    }

    private sealed class TestNewsletterEventRepository : MorWalPizVideo.Server.Services.Interfaces.INewsletterEventRepository
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
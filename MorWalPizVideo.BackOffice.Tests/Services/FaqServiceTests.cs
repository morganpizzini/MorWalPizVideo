using System.Linq.Expressions;
using FluentAssertions;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class FaqServiceTests
{
    [Fact]
    public async Task Public_projection_filters_lifecycle_and_sorts_answers_by_channel()
    {
        var fixture = CreateFixture();
        fixture.Categories.Items.Add(new FaqCategory { Id = "cat", Slug = "general", Name = "General", IsActive = true });
        fixture.Faqs.Items.Add(new Faq { Id = "faq", Question = "How?", CategoryId = "cat", Status = FaqLifecycleStatus.Published, SourceMetadata = new FaqSourceMetadata { SourceType = "Campaign", CampaignIds = ["campaign-1"] } });
        fixture.Answers.Items.Add(new FaqAnswer { Id = "answer-b", FaqId = "faq", ChannelId = "channel-b", Content = "B", Status = FaqLifecycleStatus.Published });
        fixture.Answers.Items.Add(new FaqAnswer { Id = "answer-a", FaqId = "faq", ChannelId = "channel-a", Content = "A", Status = FaqLifecycleStatus.Published });
        fixture.Answers.Items.Add(new FaqAnswer { Id = "answer-draft", FaqId = "faq", ChannelId = "channel-c", Content = "Draft", Status = FaqLifecycleStatus.Draft });

        var result = await fixture.Service.GetPublicAsync();

        result.Single().Answers.Select(answer => answer.ChannelName).Should().ContainInOrder("Alpha", "Beta");
        result.Single().Answers.Should().NotContain(answer => answer.Content == "Draft");
        result.Single().Question.Should().Be("How?");
    }

    [Fact]
    public async Task Public_projection_excludes_faqs_assigned_to_inactive_categories()
    {
        var fixture = CreateFixture();
        fixture.Categories.Items.Add(new FaqCategory { Id = "inactive", Slug = "old", Name = "Old", IsActive = false });
        fixture.Faqs.Items.Add(new Faq { Id = "faq", Question = "Hidden?", CategoryId = "inactive", Status = FaqLifecycleStatus.Published });

        (await fixture.Service.GetPublicAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Answer_list_is_scoped_to_requested_channel()
    {
        var fixture = CreateFixture();
        fixture.Answers.Items.Add(new FaqAnswer { Id = "answer-a", FaqId = "faq", ChannelId = "channel-a", Content = "A" });
        fixture.Answers.Items.Add(new FaqAnswer { Id = "answer-b", FaqId = "faq", ChannelId = "channel-b", Content = "B" });

        (await fixture.Service.GetAnswersAsync("faq", "channel-a")).Select(answer => answer.Content).Should().Equal("A");
    }

    [Fact]
    public async Task Voting_is_idempotent_and_change_vote_updates_counters()
    {
        var fixture = CreateFixture();
        fixture.Categories.Items.Add(new FaqCategory { Id = "cat", Slug = "general", Name = "General", IsActive = true });
        fixture.Faqs.Items.Add(new Faq { Id = "faq", Question = "How?", CategoryId = "cat", Status = FaqLifecycleStatus.Published });
        fixture.Answers.Items.Add(new FaqAnswer { Id = "answer", FaqId = "faq", ChannelId = "channel-a", Content = "A", Status = FaqLifecycleStatus.Published });

        (await fixture.Service.VoteAsync("faq", "Alpha", "user-1", FaqVoteValue.Helpful))!.Accepted.Should().BeTrue();
        (await fixture.Service.VoteAsync("faq", "Alpha", "user-1", FaqVoteValue.Helpful))!.Accepted.Should().BeFalse();
        (await fixture.Service.VoteAsync("faq", "Alpha", "user-1", FaqVoteValue.NotHelpful))!.Changed.Should().BeTrue();

        var answer = fixture.Answers.Items.Single();
        answer.HelpfulVotes.Should().Be(0);
        answer.NotHelpfulVotes.Should().Be(1);
    }

    [Fact]
    public async Task Voting_is_rejected_for_an_inactive_category()
    {
        var fixture = CreateFixture();
        fixture.Categories.Items.Add(new FaqCategory { Id = "inactive", Slug = "old", Name = "Old", IsActive = false });
        fixture.Faqs.Items.Add(new Faq { Id = "faq", Question = "Hidden?", CategoryId = "inactive", Status = FaqLifecycleStatus.Published });
        fixture.Answers.Items.Add(new FaqAnswer { Id = "answer", FaqId = "faq", ChannelId = "channel-a", Content = "A", Status = FaqLifecycleStatus.Published });

        (await fixture.Service.VoteAsync("answer", "user-1", FaqVoteValue.Helpful)).Should().BeNull();
    }

    [Fact]
    public async Task Answer_updates_cannot_cross_channel_boundary()
    {
        var fixture = CreateFixture();
        fixture.Answers.Items.Add(new FaqAnswer { Id = "answer", FaqId = "faq", ChannelId = "channel-a", Content = "A" });

        (await fixture.Service.UpdateAnswerAsync(new FaqAnswer { Id = "answer", FaqId = "faq", ChannelId = "channel-b", Content = "Hijack" }, "channel-b")).Should().BeNull();
        fixture.Answers.Items.Single().Content.Should().Be("A");
    }

    private static Fixture CreateFixture()
    {
        var channels = new ChannelRepository();
        channels.Items.Add(new YTChannel("channel-a", "Alpha"));
        channels.Items.Add(new YTChannel("channel-b", "Beta"));
        var faqs = new FaqRepository();
        var categories = new FaqCategoryRepository();
        var answers = new FaqAnswerRepository();
        var candidates = new FaqCandidateRepository();
        var votes = new FaqVoteRepository();
        return new(new FaqService(faqs, categories, answers, candidates, votes, channels), faqs, categories, answers);
    }

    private sealed record Fixture(FaqService Service, FaqRepository Faqs, FaqCategoryRepository Categories, FaqAnswerRepository Answers);

    private class MemoryRepository<T> : IRepository<T> where T : BaseEntity
    {
        public List<T> Items { get; } = [];
        public Task<T> GetItemAsync(string id) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id)!);
        public Task<IList<T>> GetItemsAsync() => Task.FromResult<IList<T>>(Items.ToList());
        public Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate) => Task.FromResult<IList<T>>(Items.AsQueryable().Where(predicate).ToList());
        public Task<T> AddItemAsync(T item) { Items.Add(item); return Task.FromResult(item); }
        public Task UpdateItemAsync(T item) { Items.RemoveAll(existing => existing.Id == item.Id); Items.Add(item); return Task.CompletedTask; }
        public Task DeleteItemAsync(string id) { Items.RemoveAll(item => item.Id == id); return Task.CompletedTask; }
    }

    private sealed class FaqRepository : MemoryRepository<Faq>, IFaqRepository
    {
        public Task<IList<Faq>> GetPublicAsync(string? categoryId = null) => Task.FromResult<IList<Faq>>(Items.Where(item => item.Status == FaqLifecycleStatus.Published && (categoryId == null || item.CategoryId == categoryId)).ToList());
    }
    private sealed class ChannelRepository : MemoryRepository<YTChannel>, IYTChannelRepository { }
    private sealed class FaqCategoryRepository : MemoryRepository<FaqCategory>, IFaqCategoryRepository { }
    private sealed class FaqAnswerRepository : MemoryRepository<FaqAnswer>, IFaqAnswerRepository
    {
        public Task<IList<FaqAnswer>> GetByFaqIdAsync(string faqId) => Task.FromResult<IList<FaqAnswer>>(Items.Where(item => item.FaqId == faqId).ToList());
        public Task<IList<FaqAnswer>> GetByFaqIdAndChannelIdAsync(string faqId, string channelId) => Task.FromResult<IList<FaqAnswer>>(Items.Where(item => item.FaqId == faqId && item.ChannelId == channelId).ToList());
        public Task<IList<FaqAnswer>> GetByChannelIdAsync(string channelId) => Task.FromResult<IList<FaqAnswer>>(Items.Where(item => item.ChannelId == channelId).ToList());
        public Task<int> IncrementVoteAsync(string id, FaqVoteValue value, int delta)
        {
            var item = Items.Single(item => item.Id == id);
            var updated = value == FaqVoteValue.Helpful ? item with { HelpfulVotes = item.HelpfulVotes + delta } : item with { NotHelpfulVotes = item.NotHelpfulVotes + delta };
            Items[Items.IndexOf(item)] = updated;
            return Task.FromResult(value == FaqVoteValue.Helpful ? updated.HelpfulVotes : updated.NotHelpfulVotes);
        }
        public Task<bool> SetVoteCountsAsync(string id, int helpfulVotes, int notHelpfulVotes)
        {
            var item = Items.SingleOrDefault(item => item.Id == id);
            if (item is null) return Task.FromResult(false);
            Items[Items.IndexOf(item)] = item with { HelpfulVotes = helpfulVotes, NotHelpfulVotes = notHelpfulVotes };
            return Task.FromResult(true);
        }
    }
    private sealed class FaqCandidateRepository : MemoryRepository<FaqCandidate>, IFaqCandidateRepository { }
    private sealed class FaqVoteRepository : MemoryRepository<FaqVote>, IFaqVoteRepository
    {
        public Task<FaqVote?> GetByAnswerAndUserAsync(string answerId, string userId) => Task.FromResult(Items.FirstOrDefault(item => item.AnswerId == answerId && item.UserId == userId));
        public Task<IReadOnlyList<FaqVoteCountSnapshot>> GetCountsByAnswerIdsAsync(IReadOnlyCollection<string> answerIds)
            => Task.FromResult<IReadOnlyList<FaqVoteCountSnapshot>>(Items.Where(item => answerIds.Contains(item.AnswerId)).GroupBy(item => item.AnswerId).Select(group => new FaqVoteCountSnapshot(group.Key, group.Count(item => item.Value == FaqVoteValue.Helpful), group.Count(item => item.Value == FaqVoteValue.NotHelpful), group.Max(item => item.UpdatedAt))).ToArray());
    }
}

using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class FaqVoteReconciliationServiceTests
{
    [Fact]
    public async Task Reconciliation_repairs_drift_zero_vote_and_multiple_answers()
    {
        var answers = new AnswerRepository(
            new FaqAnswer { Id = "one", HelpfulVotes = 9, NotHelpfulVotes = 2 },
            new FaqAnswer { Id = "two", HelpfulVotes = 3, NotHelpfulVotes = 4 },
            new FaqAnswer { Id = "three" });
        var votes = new VoteRepository(
            new FaqVote { Id = "v1", AnswerId = "one", UserId = "u1", Value = FaqVoteValue.Helpful },
            new FaqVote { Id = "v2", AnswerId = "one", UserId = "u2", Value = FaqVoteValue.NotHelpful },
            new FaqVote { Id = "v3", AnswerId = "one", UserId = "u3", Value = FaqVoteValue.Helpful },
            new FaqVote { Id = "v4", AnswerId = "three", UserId = "u4", Value = FaqVoteValue.NotHelpful });
        var service = CreateService(answers, votes);

        var result = await service.ReconcileAsync();

        Assert.Equal((2, 1), (answers.Items[0].HelpfulVotes, answers.Items[0].NotHelpfulVotes));
        Assert.Equal((0, 0), (answers.Items[1].HelpfulVotes, answers.Items[1].NotHelpfulVotes));
        Assert.Equal((0, 1), (answers.Items[2].HelpfulVotes, answers.Items[2].NotHelpfulVotes));
        Assert.Equal(3, result.AnswersScanned);
        Assert.Equal(3, result.AnswersRepaired);
        Assert.Equal(4, result.AuthoritativeVotes);
    }

    [Fact]
    public async Task Reconciliation_is_idempotent_and_can_be_scoped()
    {
        var answers = new AnswerRepository(
            new FaqAnswer { Id = "one", HelpfulVotes = 8 },
            new FaqAnswer { Id = "two", NotHelpfulVotes = 8 });
        var votes = new VoteRepository(new FaqVote { Id = "v1", AnswerId = "one", UserId = "u1", Value = FaqVoteValue.Helpful });
        var service = CreateService(answers, votes);

        var first = await service.ReconcileAsync(["one"]);
        var second = await service.ReconcileAsync(["one"]);

        Assert.Equal(1, first.AnswersRepaired);
        Assert.Equal(0, second.AnswersRepaired);
        Assert.Equal(0, answers.Items[1].HelpfulVotes);
        Assert.Equal(8, answers.Items[1].NotHelpfulVotes);
    }

    [Fact]
    public async Task Reconciliation_retries_when_votes_change_during_repair()
    {
        var answers = new AnswerRepository(new FaqAnswer { Id = "one" });
        var votes = new VoteRepository(new FaqVote { Id = "v1", AnswerId = "one", UserId = "u1", Value = FaqVoteValue.Helpful })
        {
            AddVoteOnSecondSnapshot = new FaqVote { Id = "v2", AnswerId = "one", UserId = "u2", Value = FaqVoteValue.NotHelpful }
        };
        var service = CreateService(answers, votes);

        var result = await service.ReconcileAsync();

        Assert.Equal(1, result.AnswersRepaired);
        Assert.Equal(1, result.ConcurrentChanges);
        Assert.Equal(1, answers.Items[0].HelpfulVotes);
        Assert.Equal(1, answers.Items[0].NotHelpfulVotes);
    }

    private static FaqVoteReconciliationService CreateService(AnswerRepository answers, VoteRepository votes)
        => new(answers, votes, NullLogger<FaqVoteReconciliationService>.Instance);

    private sealed class AnswerRepository(params FaqAnswer[] initial) : IFaqAnswerRepository
    {
        public List<FaqAnswer> Items { get; } = initial.ToList();
        public Task<FaqAnswer> GetItemAsync(string id) => Task.FromResult(Items.Single(item => item.Id == id));
        public Task<IList<FaqAnswer>> GetItemsAsync() => Task.FromResult<IList<FaqAnswer>>(Items.ToList());
        public Task<IList<FaqAnswer>> GetItemsAsync(Expression<Func<FaqAnswer, bool>> predicate) => Task.FromResult<IList<FaqAnswer>>(Items.AsQueryable().Where(predicate).ToList());
        public Task<FaqAnswer> AddItemAsync(FaqAnswer item) { Items.Add(item); return Task.FromResult(item); }
        public Task UpdateItemAsync(FaqAnswer item) { Items[Items.FindIndex(current => current.Id == item.Id)] = item; return Task.CompletedTask; }
        public Task DeleteItemAsync(string id) { Items.RemoveAll(item => item.Id == id); return Task.CompletedTask; }
        public Task<IList<FaqAnswer>> GetByFaqIdAsync(string faqId) => Task.FromResult<IList<FaqAnswer>>(Items.Where(item => item.FaqId == faqId).ToList());
        public Task<IList<FaqAnswer>> GetByFaqIdAndChannelIdAsync(string faqId, string channelId) => Task.FromResult<IList<FaqAnswer>>(Items.Where(item => item.FaqId == faqId && item.ChannelId == channelId).ToList());
        public Task<IList<FaqAnswer>> GetByChannelIdAsync(string channelId) => Task.FromResult<IList<FaqAnswer>>(Items.Where(item => item.ChannelId == channelId).ToList());
        public Task<int> IncrementVoteAsync(string id, FaqVoteValue value, int delta) => throw new NotSupportedException();
        public Task<bool> SetVoteCountsAsync(string id, int helpfulVotes, int notHelpfulVotes)
        {
            var index = Items.FindIndex(item => item.Id == id);
            if (index < 0) return Task.FromResult(false);
            Items[index] = Items[index] with { HelpfulVotes = helpfulVotes, NotHelpfulVotes = notHelpfulVotes };
            return Task.FromResult(true);
        }
    }

    private sealed class VoteRepository(params FaqVote[] initial) : IFaqVoteRepository
    {
        public List<FaqVote> Items { get; } = initial.ToList();
        public FaqVote? AddVoteOnSecondSnapshot { get; init; }
        private int snapshotCount;
        public Task<FaqVote> GetItemAsync(string id) => Task.FromResult(Items.Single(item => item.Id == id));
        public Task<IList<FaqVote>> GetItemsAsync() => Task.FromResult<IList<FaqVote>>(Items.ToList());
        public Task<IList<FaqVote>> GetItemsAsync(Expression<Func<FaqVote, bool>> predicate) => Task.FromResult<IList<FaqVote>>(Items.AsQueryable().Where(predicate).ToList());
        public Task<FaqVote> AddItemAsync(FaqVote item) { Items.Add(item); return Task.FromResult(item); }
        public Task UpdateItemAsync(FaqVote item) { Items[Items.FindIndex(current => current.Id == item.Id)] = item; return Task.CompletedTask; }
        public Task DeleteItemAsync(string id) { Items.RemoveAll(item => item.Id == id); return Task.CompletedTask; }
        public Task<FaqVote?> GetByAnswerAndUserAsync(string answerId, string userId) => Task.FromResult(Items.FirstOrDefault(item => item.AnswerId == answerId && item.UserId == userId));
        public Task<IReadOnlyList<FaqVoteCountSnapshot>> GetCountsByAnswerIdsAsync(IReadOnlyCollection<string> answerIds)
        {
            snapshotCount++;
            if (snapshotCount == 2 && AddVoteOnSecondSnapshot is not null) Items.Add(AddVoteOnSecondSnapshot);
            return Task.FromResult<IReadOnlyList<FaqVoteCountSnapshot>>(Items.Where(item => answerIds.Contains(item.AnswerId)).GroupBy(item => item.AnswerId).Select(group => new FaqVoteCountSnapshot(group.Key, group.Count(item => item.Value == FaqVoteValue.Helpful), group.Count(item => item.Value == FaqVoteValue.NotHelpful), group.Max(item => item.UpdatedAt))).ToArray());
        }
    }
}
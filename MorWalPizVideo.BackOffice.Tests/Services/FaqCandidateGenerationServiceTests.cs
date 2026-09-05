using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class FaqCandidateGenerationServiceTests
{
    [Fact]
    public async Task Generation_scopes_ai_input_and_candidates_to_selected_campaigns()
    {
        var submissions = new AskSubmissionRepository
        {
            Items =
            [
                new AskSubmission { Id = "selected", CampaignId = "campaign-1", Text = "How do I register?" },
                new AskSubmission { Id = "ignored", CampaignId = "campaign-2", Text = "This must not be sent." }
            ]
        };
        var candidates = new FaqCandidateRepository();
        IReadOnlyList<FaqCandidateSource>? receivedSources = null;
        var provider = new TestProvider(sources =>
        {
            receivedSources = sources;
            return [new FaqCandidateDraft("How do I register?", "Use the registration form.", ["campaign-1", "campaign-2"], 0.9, null)];
        });
        var service = CreateService(submissions, candidates, provider);

        var result = await service.GenerateAsync([" campaign-1 "], "reviewer");

        receivedSources.Should().ContainSingle().Which.Messages.Should().Equal("How do I register?");
        result.Should().ContainSingle();
        result[0].CampaignIds.Should().Equal("campaign-1");
        result[0].SourceSubmissionCount.Should().Be(1);
        result[0].Status.Should().Be(FaqCandidateStatus.Pending);
        result[0].CreatedBy.Should().Be("reviewer");
        submissions.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Empty_selection_is_rejected_without_reading_submissions()
    {
        var submissions = new AskSubmissionRepository();
        var provider = new TestProvider(_ => throw new InvalidOperationException("must not run"));
        var service = CreateService(submissions, new FaqCandidateRepository(), provider);

        var action = () => service.GenerateAsync([], "reviewer");

        var exception = await action.Should().ThrowAsync<FaqCandidateGenerationException>();
        exception.Which.Code.Should().Be("faq_campaign_selection_required");
        submissions.ReadCampaignIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Provider_failure_records_pending_failure_candidate_and_does_not_publish()
    {
        var submissions = new AskSubmissionRepository { Items = [new AskSubmission { CampaignId = "campaign-1", Text = "Question" }] };
        var candidates = new FaqCandidateRepository();
        var provider = new TestProvider(_ => throw new InvalidOperationException("provider unavailable"));
        var service = CreateService(submissions, candidates, provider);

        var exception = await Assert.ThrowsAsync<FaqCandidateGenerationException>(() => service.GenerateAsync(["campaign-1"], "reviewer"));

        exception.Code.Should().Be("faq_campaign_ai_failed");
        candidates.Items.Should().ContainSingle();
        candidates.Items[0].Status.Should().Be(FaqCandidateStatus.Pending);
        candidates.Items[0].FailureReason.Should().Be("faq_campaign_ai_failed");
        candidates.Items[0].Question.Should().BeEmpty();
        candidates.Items[0].Answer.Should().BeEmpty();
    }

    [Fact]
    public async Task Provider_timeout_records_pending_timeout_candidate()
    {
        var submissions = new AskSubmissionRepository { Items = [new AskSubmission { CampaignId = "campaign-1", Text = "Question" }] };
        var candidates = new FaqCandidateRepository();
        var provider = new TestProvider(_ => throw new OperationCanceledException());
        var service = CreateService(submissions, candidates, provider);

        var exception = await Assert.ThrowsAsync<FaqCandidateGenerationException>(() => service.GenerateAsync(["campaign-1"], "reviewer"));

        exception.Code.Should().Be("faq_campaign_ai_timeout");
        candidates.Items.Should().ContainSingle().Which.Status.Should().Be(FaqCandidateStatus.Pending);
    }

    [Fact]
    public async Task Successful_generation_creates_pending_candidates_only_and_does_not_mutate_submissions()
    {
        var original = new AskSubmission { Id = "submission-1", CampaignId = "campaign-1", Text = "Original", ModerationStatus = AskModerationStatus.Approved };
        var submissions = new AskSubmissionRepository { Items = [original] };
        var candidates = new FaqCandidateRepository();
        var provider = new TestProvider(_ => [new FaqCandidateDraft("Canonical question", "Draft answer", ["campaign-1"], 0.7, "faq-existing")]);
        var service = CreateService(submissions, candidates, provider);

        var result = await service.GenerateAsync(["campaign-1"], "reviewer");

        result.Single().Status.Should().Be(FaqCandidateStatus.Pending);
        result.Single().DuplicateOfFaqId.Should().Be("faq-existing");
        submissions.Items.Single().Should().Be(original);
        submissions.Items.Single().ModerationStatus.Should().Be(AskModerationStatus.Approved);
    }

    private static FaqCandidateGenerationService CreateService(AskSubmissionRepository submissions, FaqCandidateRepository candidates, TestProvider provider)
        => new(submissions, candidates, provider, NullLogger<FaqCandidateGenerationService>.Instance);

    private sealed class TestProvider(Func<IReadOnlyList<FaqCandidateSource>, IReadOnlyList<FaqCandidateDraft>> handler) : IFaqCandidateAiProvider
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<FaqCandidateDraft>> GenerateAsync(IReadOnlyList<FaqCandidateSource> sources, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(handler(sources));
        }
    }

    private sealed class AskSubmissionRepository : MemoryRepository<AskSubmission>, IAskSubmissionRepository
    {
        public List<string> ReadCampaignIds { get; } = [];
        public Task<IList<AskSubmission>> GetByCampaignIdAsync(string campaignId, int limit = 500)
        {
            ReadCampaignIds.Add(campaignId);
            return Task.FromResult<IList<AskSubmission>>(Items.Where(item => item.CampaignId == campaignId).Take(limit).ToList());
        }
        public Task<int> CountByCampaignIdAsync(string campaignId) => Task.FromResult(Items.Count(item => item.CampaignId == campaignId));
        public Task<AskSubmission?> GetByIdempotencyKeyAsync(string campaignId, string idempotencyKey) => Task.FromResult<AskSubmission?>(null);
        public Task<bool> HasRecentDuplicateAsync(string campaignId, string contentHash, DateTime since) => Task.FromResult(false);
        public Task<int> DeleteExpiredAsync(DateTime now) => Task.FromResult(0);
    }

    private sealed class FaqCandidateRepository : MemoryRepository<FaqCandidate>, IFaqCandidateRepository { }

    private abstract class MemoryRepository<T> : IRepository<T> where T : BaseEntity
    {
        public List<T> Items { get; init; } = [];
        public Task<T> GetItemAsync(string id) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id)!);
        public Task<IList<T>> GetItemsAsync() => Task.FromResult<IList<T>>(Items.ToList());
        public Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate) => Task.FromResult<IList<T>>(Items.AsQueryable().Where(predicate).ToList());
        public Task<T> AddItemAsync(T item) { Items.Add(item); return Task.FromResult(item); }
        public Task UpdateItemAsync(T item) { Items.RemoveAll(existing => existing.Id == item.Id); Items.Add(item); return Task.CompletedTask; }
        public Task DeleteItemAsync(string id) { Items.RemoveAll(item => item.Id == id); return Task.CompletedTask; }
    }
}

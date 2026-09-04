using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class AskServiceTests
{
    [Fact]
    public async Task Public_visibility_and_expiry_are_enforced()
    {
        var fixture = CreateFixture();
        var campaign = await fixture.Service.CreateAsync(PublishedCampaign(endAt: DateTime.UtcNow.AddMinutes(5)));
        var created = await fixture.Service.SubmitAsync("main", campaign.Slug, "question", "one");
        created.Should().Be(AskSubmissionResult.Created);
        (await fixture.Service.GetPublicAsync("main", campaign.Slug)).Should().NotBeNull();

        var expired = campaign with { EndAt = DateTime.UtcNow.AddMinutes(-1), Status = AskCampaignStatus.Published };
        await fixture.Campaigns.UpdateItemAsync(expired);
        (await fixture.Service.GetPublicAsync("main", campaign.Slug)).Should().BeNull();
        (await fixture.Campaigns.GetItemAsync(campaign.Id)).Status.Should().Be(AskCampaignStatus.Closed);
    }

    [Fact]
    public async Task Channel_isolation_and_slug_normalization_are_preserved()
    {
        var fixture = CreateFixture(addSecondChannel: true);
        var first = await fixture.Service.CreateAsync(PublishedCampaign(channelId: "channel-1", slug: "Same Slug"));
        var second = await fixture.Service.CreateAsync(PublishedCampaign(channelId: "channel-2", slug: "Same Slug"));
        first.Slug.Should().Be("same-slug");
        second.Slug.Should().Be(first.Slug);
        (await fixture.Service.GetPublicAsync("other", first.Slug)).Should().NotBeNull();
        (await fixture.Service.GetByIdAsync(first.Id, "channel-2")).Should().BeNull();
    }

    [Fact]
    public async Task Named_and_anonymous_policies_are_enforced()
    {
        var fixture = CreateFixture();
        var named = await fixture.Service.CreateAsync(PublishedCampaign(policy: new AskPolicy { AllowNamedSubmissions = true, NameRequired = true }));
        (await fixture.Service.SubmitAsync("main", named.Slug, "anonymous", "a")).Should().Be(AskSubmissionResult.Invalid);
        (await fixture.Service.SubmitAsync("main", named.Slug, "named", "b", "Ada")).Should().Be(AskSubmissionResult.Created);
        var anonymous = await fixture.Service.CreateAsync(PublishedCampaign(slug: "anonymous", policy: new AskPolicy()));
        (await fixture.Service.SubmitAsync("main", anonymous.Slug, "named", "c", "Ada")).Should().Be(AskSubmissionResult.Invalid);
    }

    [Fact]
    public async Task Duplicate_idempotency_and_rate_limit_are_enforced()
    {
        var fixture = CreateFixture();
        var campaign = await fixture.Service.CreateAsync(PublishedCampaign(policy: new AskPolicy { RateLimitPerHour = 2 }));
        (await fixture.Service.SubmitAsync("main", campaign.Slug, "one", "same")).Should().Be(AskSubmissionResult.Created);
        (await fixture.Service.SubmitAsync("main", campaign.Slug, "different", "same")).Should().Be(AskSubmissionResult.Duplicate);
        (await fixture.Service.SubmitAsync("main", campaign.Slug, "one", "two")).Should().Be(AskSubmissionResult.Duplicate);
        (await fixture.Service.SubmitAsync("main", campaign.Slug, "two", "three")).Should().Be(AskSubmissionResult.Created);
        (await fixture.Service.SubmitAsync("main", campaign.Slug, "three", "four")).Should().Be(AskSubmissionResult.RateLimited);
    }

    [Fact]
    public async Task Ai_assessment_decides_and_provider_failure_falls_back_to_pending()
    {
        var fixture = CreateFixture(new FixedModerationProvider(new AskModerationResult(0.1, ["abuse"], AskModerationDecision.Reject, "policy category")));
        var campaign = await fixture.Service.CreateAsync(PublishedCampaign(policy: new AskPolicy { ModerationMode = AskModerationMode.AiAssisted }));
        (await fixture.Service.SubmitAsync("main", campaign.Slug, "blocked", "one")).Should().Be(AskSubmissionResult.Created);
        (await fixture.Submissions.GetByCampaignIdAsync(campaign.Id)).Single().ModerationStatus.Should().Be(AskModerationStatus.Rejected);

        fixture = CreateFixture(new ThrowingModerationProvider());
        campaign = await fixture.Service.CreateAsync(PublishedCampaign(slug: "fallback", policy: new AskPolicy { ModerationMode = AskModerationMode.AiAssisted }));
        await fixture.Service.SubmitAsync("main", campaign.Slug, "pending", "two");
        (await fixture.Submissions.GetByCampaignIdAsync(campaign.Id)).Single().ModerationStatus.Should().Be(AskModerationStatus.Pending);
    }

    [Fact]
    public async Task Response_reaction_analytics_and_retention_are_scoped()
    {
        var fixture = CreateFixture();
        var campaign = await fixture.Service.CreateAsync(PublishedCampaign(policy: new AskPolicy { RetentionDays = 1 }));
        await fixture.Service.SubmitAsync("main", campaign.Slug, "question", "one");
        var submission = (await fixture.Submissions.GetByCampaignIdAsync(campaign.Id)).Single();
        (await fixture.Service.ModerateAsync(submission.Id, "channel-1", AskModerationStatus.Approved, "reviewed"))!.ModerationStatus.Should().Be(AskModerationStatus.Approved);
        (await fixture.Service.RespondAsync(submission.Id, "channel-1", "answer", "Team", AskResponseVisibility.Public)).Should().NotBeNull();
        (await fixture.Service.ReactAsync("main", campaign.Slug, submission.Id, "fingerprint")).Accepted.Should().BeTrue();
        (await fixture.Service.ReactAsync("main", campaign.Slug, submission.Id, "fingerprint")).Accepted.Should().BeFalse();
        (await fixture.Service.GetAnalyticsAsync(campaign.Id, "channel-1"))!.Responses.Should().Be(1);
        await fixture.Submissions.UpdateItemAsync(submission with { RetentionUntil = DateTime.UtcNow.AddMinutes(-1) });
        (await fixture.Service.DeleteExpiredAsync()).Should().Be(1);
    }

    private static AskCampaign PublishedCampaign(string channelId = "channel-1", string slug = "campaign", AskPolicy? policy = null, DateTime? endAt = null)
        => new() { ChannelId = channelId, Title = "Ask", Slug = slug, Status = AskCampaignStatus.Published, Policy = policy ?? new(), EndAt = endAt };

    private static Fixture CreateFixture(IAskModerationProvider? provider = null, bool addSecondChannel = false)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ask:RecaptchaRequired"] = "false",
            ["Ask:RateLimitPerHour"] = "5"
        }).Build();
        var campaigns = new MemoryCampaignRepository();
        var submissions = new MemorySubmissionRepository();
        var reactions = new MemoryReactionRepository();
        var channels = new MemoryChannelRepository();
        channels.Items.Add(new YTChannel("channel-1", "main"));
        if (addSecondChannel) channels.Items.Add(new YTChannel("channel-2", "other"));
        return new Fixture(new AskService(campaigns, submissions, reactions, channels, configuration, provider ?? new UnavailableAskModerationProvider(), NullLogger<AskService>.Instance), campaigns, submissions);
    }

    private sealed record Fixture(AskService Service, MemoryCampaignRepository Campaigns, MemorySubmissionRepository Submissions);

    private sealed class MemoryCampaignRepository : MemoryRepository<AskCampaign>, IAskCampaignRepository
    {
        public Task<AskCampaign?> GetByChannelAndSlugAsync(string channelId, string slug) => Task.FromResult(Items.FirstOrDefault(x => x.ChannelId == channelId && x.Slug == slug));
        public Task<IList<AskCampaign>> GetByChannelIdAsync(string channelId) => Task.FromResult<IList<AskCampaign>>(Items.Where(x => x.ChannelId == channelId).ToList());
    }

    private sealed class MemoryChannelRepository : MemoryRepository<YTChannel>, IYTChannelRepository
    {
    }

    private class MemoryRepository<T> : IRepository<T> where T : BaseEntity
    {
        public List<T> Items { get; } = [];
        public Task<T> GetItemAsync(string id) => Task.FromResult(Items.Single(x => x.Id == id));
        public Task<IList<T>> GetItemsAsync() => Task.FromResult<IList<T>>(Items.ToList());
        public Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate) => Task.FromResult<IList<T>>(Items.AsQueryable().Where(predicate).ToList());
        public Task<T> AddItemAsync(T item) { Items.Add(item); return Task.FromResult(item); }
        public Task UpdateItemAsync(T item) { Items.RemoveAll(x => x.Id == item.Id); Items.Add(item); return Task.CompletedTask; }
        public Task DeleteItemAsync(string id) { Items.RemoveAll(x => x.Id == id); return Task.CompletedTask; }
    }

    private sealed class MemorySubmissionRepository : MemoryRepository<AskSubmission>, IAskSubmissionRepository
    {
        public Task<IList<AskSubmission>> GetByCampaignIdAsync(string id, int limit = 500) => Task.FromResult<IList<AskSubmission>>(Items.Where(x => x.CampaignId == id).Take(limit).ToList());
        public Task<int> CountByCampaignIdAsync(string id) => Task.FromResult(Items.Count(x => x.CampaignId == id));
        public Task<AskSubmission?> GetByIdempotencyKeyAsync(string id, string key) => Task.FromResult(Items.FirstOrDefault(x => x.CampaignId == id && x.IdempotencyKey == key));
        public Task<bool> HasRecentDuplicateAsync(string id, string hash, DateTime since) => Task.FromResult(Items.Any(x => x.CampaignId == id && x.ContentHash == hash && x.SubmittedAt >= since));
        public Task<int> DeleteExpiredAsync(DateTime now) { var expired = Items.Where(x => x.RetentionUntil <= now).ToList(); expired.ForEach(x => Items.Remove(x)); return Task.FromResult(expired.Count); }
    }

    private sealed class MemoryReactionRepository : MemoryRepository<AskReaction>, IAskReactionRepository
    {
        public Task<bool> ExistsAsync(string id, string fingerprint) => Task.FromResult(Items.Any(x => x.SubmissionId == id && x.Fingerprint == fingerprint));
        public Task<int> CountBySubmissionIdAsync(string id) => Task.FromResult(Items.Count(x => x.SubmissionId == id));
    }

    private sealed class FixedModerationProvider(AskModerationResult result) : IAskModerationProvider
    {
        public Task<AskModerationResult?> AssessAsync(string content, CancellationToken cancellationToken = default) => Task.FromResult<AskModerationResult?>(result);
    }

    private sealed class ThrowingModerationProvider : IAskModerationProvider
    {
        public Task<AskModerationResult?> AssessAsync(string content, CancellationToken cancellationToken = default) => throw new InvalidOperationException("provider unavailable");
    }
}

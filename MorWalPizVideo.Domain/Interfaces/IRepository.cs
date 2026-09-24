using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Models.Models;
using System.Linq.Expressions;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T> GetItemAsync(string id);
        Task<IList<T>> GetItemsAsync();
        Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddItemAsync(T item);
        Task UpdateItemAsync(T item);
        Task DeleteItemAsync(string id);
    }
    public interface IYouTubeContentRepository : IRepository<YouTubeContent>
    {
        Task<bool> UpdateMutableFieldsAsync(YouTubeContent entity);
        Task<VideoReferenceAppendResult> AddVideoReferenceAsync(string matchId, VideoRef videoReference);
        Task<bool> RemoveVideoReferenceAsync(string matchId, string youtubeId);
        Task<bool> RemoveVideoReferenceAsync(string matchId, VideoRef expectedReference);
        Task<bool> RemoveEmbeddedYouTubeLinksAsync(string matchId);
        Task<IList<VideoPublication>> GetPublicationsAsync(DateTime fromInclusive, DateTime toExclusive, string? channelId = null);
        Task<IList<YouTubeContent>> GetOwnedAsync(string userId, IList<string> channelIds);
        Task<IList<YouTubeContent>> GetPublicOrderedAsync(bool includePrivate, int skip, int take);
        Task<long> CountPublicAsync(bool includePrivate);
        Task<IList<YouTubeContent>> GetPublicOrderedForChannelAsync(string channelId, int skip, int take);
        Task<long> CountPublicForChannelAsync(string channelId);
        Task<YouTubeContent?> GetByUrlAsync(string url, bool includePrivate);
        Task<IList<YouTubeContent>> GetByIdsAsync(IList<string> ids, bool includePrivate);
    }

    public interface ISocialAssetRepository : IRepository<SocialAsset>
    {
        Task<SocialAsset?> GetByIdempotencyKeyAsync(string channelId, string idempotencyKey);
    }

    public enum VideoReferenceAppendResult
    {
        Added,
        Duplicate,
        NotFound
    }

    public sealed record VideoReferenceAppendOutcome(
        VideoReferenceAppendResult AppendResult,
        string? CacheStatus,
        string? CacheError = null);

    public sealed record VideoPublication(string VideoId, string Title, DateTime PublishedAt);
    public interface IProductRepository : IRepository<Product>
    {
        Task<IList<Product>> GetPublicOrderedAsync(int skip, int take);
        Task<IList<Product>> GetPublicOrderedAsync(string channelId, int skip, int take);
    }
    public interface IProductCategoryRepository : IRepository<ProductCategory> { }
    public interface IYTChannelRepository : IRepository<YTChannel> { }
    public interface ISponsorRepository : IRepository<Sponsor> { }
    public interface ISponsorApplyRepository : IRepository<SponsorApply> { }
    public interface IPageRepository : IRepository<Page>
    {
        Task<Page?> GetByUrlAsync(string url, string? channelId = null);
    }
    public interface IChannelNavigationRepository : IRepository<ChannelNavigation>
    {
        Task<ChannelNavigation?> GetByChannelIdAsync(string channelId);
    }
    public interface IQueryLinkRepository : IRepository<QueryLink> { }
    public interface IPublishScheduleRepository : IRepository<PublishSchedule> { }
    public interface ICalendarEventRepository : IRepository<CalendarEvent>
    {
        Task<IList<CalendarEvent>> GetRecentAsync(DateTime fromInclusive, int limit);
    }
    public interface ICompilationRepository : IRepository<Compilation>
    {
        Task<Compilation?> GetByUrlAsync(string url);
    }
    public interface IShortLinkRepository : IRepository<ShortLink>
    {
        // Indexed canonical lookup by normalized code; comparison is case-insensitive for legacy compatibility.
        Task<ShortLink?> GetByCodeAsync(string code);
        // Atomically replace the canonical record for a normalized code, or insert it when absent.
        Task<ShortLink> ReplaceByNormalizedCodeAsync(ShortLink link, CancellationToken cancellationToken = default);
        // Atomic counter increment, avoiding the read-modify-replace race on click tracking.
        Task<int> IncrementClicksAsync(string id);
        Task<ShortLink?> GetByCampaignIdAsync(string campaignId);
    }
    public interface IQuickLinksRepository : IRepository<QuickLinks>
    {
        Task<QuickLinks?> GetByUrlAsync(string url);
    }
    public interface IChannelNewsRepository : IRepository<ChannelNews> { }
    public interface IConfigurationRepository : IRepository<MorWalPizConfiguration> { }
    public interface ICategoryRepository : IRepository<Category>
    {
    }
    public interface ICustomFormRepository : IRepository<CustomForm>
    {
        Task<IList<CustomForm>> GetActiveAsync(string? channelId = null);
        Task<CustomForm?> GetByUrlAsync(string url, string? channelId = null);
        Task<IList<CustomForm>> GetBatchAsync(string? continuationToken, int batchSize);
    }

    public interface ICustomFormResponseRepository : IRepository<CustomFormResponseDocument>
    {
        Task<IList<CustomFormResponseDocument>> GetByFormIdAsync(string formId, int limit = 500);
        Task<int> CountByFormIdAsync(string formId);
        Task<bool> ExistsForFormAsync(string formId);
        Task<bool> UpsertByFormAndResponseIdAsync(CustomFormResponseDocument item);
        Task<IList<CustomFormResponseDocument>> ClaimBatchAsync(DateTime now, DateTime leaseUntil, string workerId, int batchSize);
        Task<bool> MarkProcessedAsync(string formId, string responseId, string workerId);
        Task<bool> MarkSkippedAsync(string formId, string responseId, string workerId, string reason);
        Task<bool> MarkFailedAsync(string formId, string responseId, string workerId, string error);
    }

    public interface ISurveyRepository : IRepository<Survey>
    {
        Task<IList<Survey>> GetEligibleAsync(string channelId, DateTime utcNow);
        Task<Survey?> GetByUrlAsync(string url, string channelId);
        Task<bool> ExistsReferencingFormAsync(string formId, string channelId);
    }

    public interface IAskCampaignRepository : IRepository<AskCampaign>
    {
        Task<AskCampaign?> GetByChannelAndSlugAsync(string channelId, string slug);
        Task<IList<AskCampaign>> GetByChannelIdAsync(string channelId);
    }

    public interface IAskSubmissionRepository : IRepository<AskSubmission>
    {
        Task<IList<AskSubmission>> GetByCampaignIdAsync(string campaignId, int limit = 500);
        Task<int> CountByCampaignIdAsync(string campaignId);
        Task<AskSubmission?> GetByIdempotencyKeyAsync(string campaignId, string idempotencyKey);
        Task<bool> HasRecentDuplicateAsync(string campaignId, string contentHash, DateTime since);
        Task<int> DeleteExpiredAsync(DateTime now);
    }
    public interface IAskReactionRepository : IRepository<AskReaction>
    {
        Task<bool> ExistsAsync(string submissionId, string fingerprint);
        Task<int> CountBySubmissionIdAsync(string submissionId);
    }
    public interface IFaqRepository : IRepository<Faq>
    {
        Task<IList<Faq>> GetPublicAsync(string? categoryId = null);
    }
    public interface IFaqCategoryRepository : IRepository<FaqCategory> { }
    public interface IFaqAnswerRepository : IRepository<FaqAnswer>
    {
        Task<IList<FaqAnswer>> GetByFaqIdAsync(string faqId);
        Task<IList<FaqAnswer>> GetByFaqIdAndChannelIdAsync(string faqId, string channelId);
        Task<IList<FaqAnswer>> GetByChannelIdAsync(string channelId);
        Task<int> IncrementVoteAsync(string id, FaqVoteValue value, int delta);
        Task<bool> SetVoteCountsAsync(string id, int helpfulVotes, int notHelpfulVotes);
    }
    public interface IFaqCandidateRepository : IRepository<FaqCandidate> { }
    public interface IFaqVoteRepository : IRepository<FaqVote>
    {
        Task<FaqVote?> GetByAnswerAndUserAsync(string answerId, string userId);
        Task<IReadOnlyList<FaqVoteCountSnapshot>> GetCountsByAnswerIdsAsync(IReadOnlyCollection<string> answerIds);
    }

    public sealed record FaqVoteCountSnapshot(string AnswerId, int HelpfulVotes, int NotHelpfulVotes, DateTime LatestUpdatedAt)
    {
        public int TotalVotes => HelpfulVotes + NotHelpfulVotes;
    }

    // Insights repositories
    public interface IInsightTopicRepository : IRepository<InsightTopic> { }
    public interface IInsightNewsItemRepository : IRepository<InsightNewsItem> { }
    public interface IInsightContentPlanRepository : IRepository<InsightContentPlan> { }
    public interface IInsightSourceCursorRepository : IRepository<InsightSourceCursor> { }
    public interface IInsightCommentAnalysisRunRepository : IRepository<InsightCommentAnalysisRun> { }

    // Shop repositories
    public interface IDigitalProductRepository : IRepository<DigitalProduct>
    {
        Task<IList<DigitalProduct>> GetByCategoryIdAsync(string categoryId, int limit = 500);
        Task<IList<DigitalProduct>> GetPublicCatalogAsync(int skip, int take);
    }
    public interface IDigitalProductCategoryRepository : IRepository<DigitalProductCategory>
    {
        Task<IList<DigitalProductCategory>> GetOrderedAsync(int skip, int take);
    }
    public interface ICustomerRepository : IRepository<Customer> { }
    public interface ICartRepository : IRepository<Cart> { }

    // Shooting ITA repositories
    public interface ICompetitionRepository : IRepository<Competition> { }
    public interface IUserChannelRepository : IRepository<UserChannel>
    {
        Task<IList<UserChannel>> GetByUserIdAsync(string userId);
        Task<IList<UserChannel>> GetByChannelIdAsync(string channelId);
        Task<UserChannel?> GetByUserAndChannelAsync(string userId, string channelId);
    }
    public interface IUserChannelOwnerRepository : IRepository<UserChannelOwner>
    {
        Task<IList<UserChannelOwner>> GetByUserIdAsync(string userId);
        Task<IList<UserChannelOwner>> GetByChannelIdAsync(string channelId);
    }
    public interface IUserRequestRepository : IRepository<UserRequest> { }
    public interface INewsletterRepository : IRepository<Newsletter>
    {
        Task<Newsletter?> ClaimForSendingAsync(string channelId, string newsletterId, NewsletterState expectedState, DateTime now, DateTime? expectedScheduledAtUtc = null, CancellationToken cancellationToken = default);
        Task<IList<Newsletter>> GetDueScheduledAsync(DateTime now, int limit, CancellationToken cancellationToken = default);
    }
    public interface INewsletterTemplateRepository : IRepository<NewsletterTemplate> { }
    public interface INewsletterUserRepository : IRepository<NewsletterUser>
    {
        Task<NewsletterUser?> ConsumeConfirmationAsync(string channelId, string tokenHash, DateTime now, CancellationToken cancellationToken = default);
        Task<NewsletterUser?> ConsumeUnsubscribeAsync(string channelId, string tokenHash, DateTime now, CancellationToken cancellationToken = default);
    }
    public interface INewsletterRecipientRepository : IRepository<NewsletterRecipient> { }
    public interface INewsletterRecipientDispatchRepository
    {
        Task EnsurePendingAsync(NewsletterRecipient recipient, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<NewsletterRecipient>> ClaimBatchAsync(string channelId, string newsletterId, int batchSize, DateTime now, TimeSpan lease, CancellationToken cancellationToken = default);
        Task MarkSentAsync(string recipientId, string? providerMessageId, DateTime sentAt, CancellationToken cancellationToken = default);
        Task MarkSuppressedAsync(string recipientId, string reason, DateTime suppressedAt, CancellationToken cancellationToken = default);
        Task MarkFailedAsync(string recipientId, string reason, bool retryable, DateTime failedAt, CancellationToken cancellationToken = default);
    }
    public interface INewsletterEventRepository : IRepository<NewsletterEvent>
    {
        Task RecordClickAsync(string channelId, string newsletterId, string shortLinkCode, DateTime occurredAt, CancellationToken cancellationToken = default);
    }

    public interface IUserGroupRepository : IRepository<UserGroup>
    {
        Task<UserGroup?> GetByCodeAsync(string code);
        Task<IList<UserGroup>> GetByIdsAsync(IList<string> groupIds);
    }

    public interface IImpersonationGrantRepository : IRepository<ImpersonationGrant>
    {
        Task<ImpersonationGrant?> GetByHashAsync(string grantHash);
        Task<ImpersonationGrant?> RedeemAsync(string grantHash, string sessionId, DateTime redeemedAt);
    }

    public interface IImpersonationSessionRepository : IRepository<ImpersonationSession>
    {
        Task<ImpersonationSession?> GetByHashAsync(string sessionHash);
        Task<bool> EndAsync(string sessionHash, DateTime endedAt, string reason);
    }

    public interface IImpersonationAuditRepository : IRepository<ImpersonationAuditEvent>;
    public interface IAuditEventRepository : IRepository<AuditEvent>;
}

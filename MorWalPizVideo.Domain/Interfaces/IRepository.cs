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
        Task<IList<YouTubeContent>> GetOwnedAsync(string userId, IList<string> channelIds);
        Task<IList<YouTubeContent>> GetPublicOrderedAsync(bool includePrivate, int skip, int take);
        Task<long> CountPublicAsync(bool includePrivate);
        Task<YouTubeContent?> GetByUrlAsync(string url, bool includePrivate);
        Task<IList<YouTubeContent>> GetByIdsAsync(IList<string> ids, bool includePrivate);
    }
    public interface IProductRepository : IRepository<Product>
    {
        Task<IList<Product>> GetPublicOrderedAsync(int skip, int take);
    }
    public interface IProductCategoryRepository : IRepository<ProductCategory> { }
    public interface IYTChannelRepository : IRepository<YTChannel> { }
    public interface ISponsorRepository : IRepository<Sponsor> { }
    public interface ISponsorApplyRepository : IRepository<SponsorApply> { }
    public interface IPageRepository : IRepository<Page>
    {
        Task<Page?> GetByUrlAsync(string url);
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
    public interface IBioLinkRepository : IRepository<BioLink> { }
    public interface IShortLinkRepository : IRepository<ShortLink>
    {
        // Indexed canonical lookup by normalized code; comparison is case-insensitive for legacy compatibility.
        Task<ShortLink?> GetByCodeAsync(string code);
        // Atomic counter increment, avoiding the read-modify-replace race on click tracking.
        Task<int> IncrementClicksAsync(string id);
    }
    public interface IConfigurationRepository : IRepository<MorWalPizConfiguration> { }
    public interface ICategoryRepository : IRepository<Category>
    {
    }
    public interface ICustomFormRepository : IRepository<CustomForm>
    {
        Task<IList<CustomForm>> GetActiveAsync();
        Task<CustomForm?> GetByUrlAsync(string url);
        Task<IList<CustomForm>> GetBatchAsync(string? continuationToken, int batchSize);
    }

    public interface ICustomFormResponseRepository : IRepository<CustomFormResponseDocument>
    {
        Task<IList<CustomFormResponseDocument>> GetByFormIdAsync(string formId, int limit = 500);
        Task<int> CountByFormIdAsync(string formId);
        Task<bool> ExistsForFormAsync(string formId);
        Task<bool> UpsertByFormAndResponseIdAsync(CustomFormResponseDocument item);
    }

    // Insights repositories
    public interface IInsightTopicRepository : IRepository<InsightTopic> { }
    public interface IInsightNewsItemRepository : IRepository<InsightNewsItem> { }
    public interface IInsightContentPlanRepository : IRepository<InsightContentPlan> { }
    public interface IInsightSourceCursorRepository : IRepository<InsightSourceCursor> { }

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

    public interface IUserGroupRepository : IRepository<UserGroup>
    {
        Task<UserGroup?> GetByCodeAsync(string code);
        Task<IList<UserGroup>> GetByIdsAsync(IList<string> groupIds);
    }
}

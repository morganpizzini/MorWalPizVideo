using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using System.Security.Cryptography;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public class MatchMockRepository : BaseMockRepository<YouTubeContent>, IYouTubeContentRepository
    {
        public MatchMockRepository(IMockScenario scenario) : base(scenario, "matches")
        {
        }

        public async Task<IList<VideoPublication>> GetPublicationsAsync(DateTime fromInclusive, DateTime toExclusive, string? channelId = null)
            => (await GetItemsAsync())
                .Where(match => !match.IsPrivate)
            .Where(match => string.IsNullOrWhiteSpace(channelId) || match.OwnerChannelId == channelId || match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId)))
                .SelectMany(match => match.VideoRefs ?? [])
                .Where(video => video.PublishedAt >= fromInclusive && video.PublishedAt < toExclusive)
                .Select(video => new VideoPublication(video.YoutubeId, video.Title, video.PublishedAt))
                .OrderBy(video => video.PublishedAt)
                .ToList();

        public async Task<IList<YouTubeContent>> GetOwnedAsync(string userId, IList<string> channelIds)
            => await GetItemsAsync(match =>
                match.CreatorUserId == userId ||
                match.VideoRefs.Any(video => video.ChannelIds.Any(channelIds.Contains)));

        public async Task<IList<YouTubeContent>> GetPublicOrderedAsync(bool includePrivate, int skip, int take)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 200);
            var all = await GetItemsAsync();
            var query = all.AsEnumerable();
            if (!includePrivate)
            {
                query = query.Where(x => !x.IsPrivate);
            }

            return query
                .OrderByDescending(x => x.CreationDateTime)
                .Skip(safeSkip)
                .Take(safeTake)
                .ToList();
        }

        public async Task<long> CountPublicAsync(bool includePrivate)
        {
            var all = await GetItemsAsync();
            return includePrivate ? all.Count : all.Count(x => !x.IsPrivate);
        }

        public async Task<YouTubeContent?> GetByUrlAsync(string url, bool includePrivate)
        {
            var all = await GetItemsAsync();
            var query = all.Where(x => x.Url == url);
            if (!includePrivate)
            {
                query = query.Where(x => !x.IsPrivate).ToList();
            }

            return query.FirstOrDefault();
        }

        public async Task<IList<YouTubeContent>> GetByIdsAsync(IList<string> ids, bool includePrivate)
        {
            if (ids.Count == 0)
            {
                return [];
            }

            var all = await GetItemsAsync();
            var query = all.Where(x => ids.Contains(x.Id));
            if (!includePrivate)
            {
                query = query.Where(x => !x.IsPrivate).ToList();
            }

            return query.ToList();
        }
    }
    public class PageMockRepository : BaseMockRepository<Page>, IPageRepository
    {
        public PageMockRepository(IMockScenario scenario) : base(scenario, "pages")
        {
        }

        public async Task<Page?> GetByUrlAsync(string url)
            => (await GetItemsAsync(x => x.Url == url)).FirstOrDefault();
    }
    public class ProductMockRepository : BaseMockRepository<Product>, IProductRepository
    {
        public ProductMockRepository(IMockScenario scenario) : base(scenario, "products")
        {
        }

        public async Task<IList<Product>> GetPublicOrderedAsync(int skip, int take)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 500);
            return (await GetItemsAsync())
                .OrderByDescending(x => x.CreationDateTime)
                .Skip(safeSkip)
                .Take(safeTake)
                .ToList();
        }
    }

    public class ProductCategoryMockRepository : BaseMockRepository<ProductCategory>, IProductCategoryRepository
    {
        public ProductCategoryMockRepository(IMockScenario scenario) : base(scenario, "productCategories")
        {
        }
    }

    public class ConfigurationMockRepository : BaseMockRepository<MorWalPizConfiguration>, IConfigurationRepository
    {
        public ConfigurationMockRepository(IMockScenario scenario) : base(scenario, "configurations")
        {
        }
    }

    public class SponsorMockRepository : BaseMockRepository<Sponsor>, ISponsorRepository
    {
        public SponsorMockRepository(IMockScenario scenario) : base(scenario, "sponsors")
        {
        }
    }
    public class YTChannelMockRepository : BaseMockRepository<YTChannel>, IYTChannelRepository
    {
        public YTChannelMockRepository(IMockScenario scenario) : base(scenario, "ytchannels")
        {
        }
    }
    public class CalendarEventMockRepository : BaseMockRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventMockRepository(IMockScenario scenario) : base(scenario, "calendarEvents")
        {
        }

        public async Task<IList<CalendarEvent>> GetRecentAsync(DateTime fromInclusive, int limit)
        {
            var safeLimit = Math.Clamp(limit, 1, 250);
            return (await GetItemsAsync(x => x.CreationDateTime >= fromInclusive))
                .OrderByDescending(x => x.CreationDateTime)
                .Take(safeLimit)
                .ToList();
        }
    }

    public class CompilationMockRepository : BaseMockRepository<Compilation>, ICompilationRepository
    {
        public CompilationMockRepository(IMockScenario scenario) : base(scenario, "compilations")
        {
        }

        public async Task<Compilation?> GetByUrlAsync(string url)
            => (await GetItemsAsync(x => x.Url == url)).FirstOrDefault();
    }

    public class BioLinkMockRepository : BaseMockRepository<BioLink>, IBioLinkRepository
    {
        public BioLinkMockRepository(IMockScenario scenario) : base(scenario, "bioLinks") { }
    }

    public class SponsorApplyMockRepository : BaseMockRepository<SponsorApply>, ISponsorApplyRepository
    {
        public SponsorApplyMockRepository(IMockScenario scenario) : base(scenario, "sponsorApplies") { }
    }

    public class ShortLinkMockRepository : BaseMockRepository<ShortLink>, IShortLinkRepository
    {
        public ShortLinkMockRepository(IMockScenario scenario) : base(scenario, "shortLinks")
        {
        }

        public async Task<ShortLink?> GetByCodeAsync(string code)
        {
            var normalizedCode = code.Trim().ToLowerInvariant();
            return (await GetItemsAsync(x => x.Code.ToLowerInvariant() == normalizedCode)).FirstOrDefault();
        }

        public async Task<int> IncrementClicksAsync(string id)
        {
            var item = await GetItemAsync(id);
            if (item == null)
                return 0;

            item.ClicksCount += 1;
            await UpdateItemAsync(item);
            return item.ClicksCount;
        }
    }

    public class CategoryMockRepository : BaseMockRepository<Category>, ICategoryRepository
    {
        public CategoryMockRepository(IMockScenario scenario) : base(scenario, "categories")
        {
        }
    }
    public class QueryLinkMockRepository : BaseMockRepository<QueryLink>, IQueryLinkRepository
    {
        public QueryLinkMockRepository(IMockScenario scenario) : base(scenario, "queryLinks")
        {
        }
    }

    public class UserMockRepository : BaseMockRepository<User>, IUserRepository
    {
        public UserMockRepository(IMockScenario scenario) : base(scenario, "users")
        {
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var u = username.ToLower();
            var user = (await GetItemsAsync(x => x.Username.ToLower() == u || x.Email.ToLower() == u)).FirstOrDefault();
            if (user == null || !user.IsActive)
                return null;

            return UserRepository.VerifyPassword(password, user.PasswordHash, user.Salt) ? user : null;
        }
    }

    public class UserGroupMockRepository : BaseMockRepository<UserGroup>, IUserGroupRepository
    {
        public UserGroupMockRepository(IMockScenario scenario) : base(scenario, "userGroups")
        {
        }

        public async Task<UserGroup?> GetByCodeAsync(string code)
        {
            var normalizedCode = code.Trim().ToLowerInvariant();
            return (await GetItemsAsync(group => group.Code.ToLower() == normalizedCode)).FirstOrDefault();
        }

        public async Task<IList<UserGroup>> GetByIdsAsync(IList<string> groupIds)
        {
            if (groupIds.Count == 0)
            {
                return [];
            }

            return await GetItemsAsync(group => groupIds.Contains(group.Id));
        }
    }

    public class ImpersonationGrantMockRepository : BaseMockRepository<ImpersonationGrant>, IImpersonationGrantRepository
    {
        private static readonly object RedemptionSync = new();

        public ImpersonationGrantMockRepository(IMockScenario scenario) : base(scenario, "impersonationGrants")
        {
        }

        public async Task<ImpersonationGrant?> GetByHashAsync(string grantHash)
            => (await GetItemsAsync(x => x.GrantHash == grantHash)).FirstOrDefault();

        public Task<ImpersonationGrant?> RedeemAsync(string grantHash, string sessionId, DateTime redeemedAt)
        {
            lock (RedemptionSync)
            {
                var grant = GetItemsAsync(x => x.GrantHash == grantHash &&
                    x.RedeemedAt == null && x.ExpiresAt > redeemedAt).GetAwaiter().GetResult().FirstOrDefault();
                if (grant is null)
                {
                    return Task.FromResult<ImpersonationGrant?>(null);
                }

                var redeemed = grant with { RedeemedAt = redeemedAt, SessionId = sessionId };
                UpdateItemAsync(redeemed).GetAwaiter().GetResult();
                return Task.FromResult<ImpersonationGrant?>(redeemed);
            }
        }
    }

    public class ImpersonationSessionMockRepository : BaseMockRepository<ImpersonationSession>, IImpersonationSessionRepository
    {
        private static readonly object EndSync = new();

        public ImpersonationSessionMockRepository(IMockScenario scenario) : base(scenario, "impersonationSessions")
        {
        }

        public async Task<ImpersonationSession?> GetByHashAsync(string sessionHash)
            => (await GetItemsAsync(x => x.SessionHash == sessionHash)).FirstOrDefault();

        public Task<bool> EndAsync(string sessionHash, DateTime endedAt, string reason)
        {
            lock (EndSync)
            {
                var session = GetItemsAsync(x => x.SessionHash == sessionHash && x.EndedAt == null)
                    .GetAwaiter().GetResult().FirstOrDefault();
                if (session is null)
                {
                    return Task.FromResult(false);
                }

                UpdateItemAsync(session with { EndedAt = endedAt, EndReason = reason })
                    .GetAwaiter().GetResult();
                return Task.FromResult(true);
            }
        }
    }

    public class ImpersonationAuditMockRepository : BaseMockRepository<ImpersonationAuditEvent>, IImpersonationAuditRepository
    {
        public ImpersonationAuditMockRepository(IMockScenario scenario) : base(scenario, "impersonationAuditEvents")
        {
        }
    }

    public class LoginAttemptMockRepository : BaseMockRepository<LoginAttempt>, ILoginAttemptRepository
    {
        public LoginAttemptMockRepository(IMockScenario scenario) : base(scenario, "loginAttempts")
        {
        }

        public async Task<List<LoginAttempt>> GetRecentAttemptsByIpAsync(string ipAddress, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var items = await GetItemsAsync();
            return items.Where(a => a.IpAddress == ipAddress && a.AttemptTime >= cutoffTime)
                       .OrderByDescending(a => a.AttemptTime)
                       .ToList();
        }

        public async Task<List<LoginAttempt>> GetRecentAttemptsByUsernameAsync(string username, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var items = await GetItemsAsync();
            return items.Where(a => a.Username == username && a.AttemptTime >= cutoffTime)
                       .OrderByDescending(a => a.AttemptTime)
                       .ToList();
        }

        public async Task<int> GetFailedAttemptsCountByIpAsync(string ipAddress, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var items = await GetItemsAsync();
            return items.Count(a => a.IpAddress == ipAddress && !a.IsSuccessful && a.AttemptTime >= cutoffTime);
        }

        public async Task<int> GetFailedAttemptsCountByUsernameAsync(string username, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var items = await GetItemsAsync();
            return items.Count(a => a.Username == username && !a.IsSuccessful && a.AttemptTime >= cutoffTime);
        }

        public async Task<DateTime?> GetLastFailedAttemptTimeByIpAsync(string ipAddress)
        {
            var items = await GetItemsAsync();
            var lastAttempt = items.Where(a => a.IpAddress == ipAddress && !a.IsSuccessful)
                                  .OrderByDescending(a => a.AttemptTime)
                                  .FirstOrDefault();
            return lastAttempt?.AttemptTime;
        }

        public async Task<DateTime?> GetLastFailedAttemptTimeByUsernameAsync(string username)
        {
            var items = await GetItemsAsync();
            var lastAttempt = items.Where(a => a.Username == username && !a.IsSuccessful)
                                  .OrderByDescending(a => a.AttemptTime)
                                  .FirstOrDefault();
            return lastAttempt?.AttemptTime;
        }

        public async Task CleanupOldAttemptsAsync(TimeSpan olderThan)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(olderThan);
            var items = await GetItemsAsync();
            var itemsToRemove = items.Where(a => a.AttemptTime < cutoffTime).ToList();
            foreach (var item in itemsToRemove)
            {
                await DeleteItemAsync(item.Id!);
            }
        }
    }

    public class PublishScheduleMockRepository : BaseMockRepository<PublishSchedule>, IPublishScheduleRepository
    {
        public PublishScheduleMockRepository(IMockScenario scenario) : base(scenario, "publishSchedules")
        {
        }
    }

    public class CustomFormMockRepository : BaseMockRepository<CustomForm>, ICustomFormRepository
    {
        public CustomFormMockRepository(IMockScenario scenario) : base(scenario, "customForms")
        {
        }

        public Task<IList<CustomForm>> GetActiveAsync() => GetItemsAsync(x => x.Active);

        public async Task<CustomForm?> GetByUrlAsync(string url)
            => (await GetItemsAsync(x => x.Url.ToLower() == url.ToLower())).FirstOrDefault();

        public async Task<IList<CustomForm>> GetBatchAsync(string? continuationToken, int batchSize)
        {
            var safeBatchSize = Math.Clamp(batchSize, 1, 200);
            var forms = await GetItemsAsync();
            var ordered = forms.OrderBy(x => x.Id, StringComparer.Ordinal);

            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                ordered = ordered.Where(x => string.CompareOrdinal(x.Id, continuationToken) > 0)
                    .OrderBy(x => x.Id, StringComparer.Ordinal);
            }

            return ordered.Take(safeBatchSize).ToList();
        }
    }

    public class CustomFormResponseMockRepository : BaseMockRepository<CustomFormResponseDocument>, ICustomFormResponseRepository
    {
        public CustomFormResponseMockRepository(IMockScenario scenario) : base(scenario, "customFormResponses")
        {
        }

        public async Task<IList<CustomFormResponseDocument>> GetByFormIdAsync(string formId, int limit = 500)
        {
            var safeLimit = Math.Clamp(limit, 1, 5000);
            return (await GetItemsAsync(x => x.FormId == formId))
                .OrderByDescending(x => x.SubmittedAt)
                .Take(safeLimit)
                .ToList();
        }

        public async Task<int> CountByFormIdAsync(string formId)
            => (await GetItemsAsync(x => x.FormId == formId)).Count;

        public async Task<bool> ExistsForFormAsync(string formId)
            => (await GetItemsAsync(x => x.FormId == formId)).Count > 0;

        public async Task<bool> UpsertByFormAndResponseIdAsync(CustomFormResponseDocument item)
        {
            var existing = (await GetItemsAsync(x => x.FormId == item.FormId && x.ResponseId == item.ResponseId)).FirstOrDefault();
            if (existing == null)
            {
                await AddItemAsync(item);
                return true;
            }

            await UpdateItemAsync(item with { Id = existing.Id });
            return false;
        }
    }

    public class DigitalProductMockRepository : BaseMockRepository<DigitalProduct>, IDigitalProductRepository
    {
        public DigitalProductMockRepository(IMockScenario scenario) : base(scenario, "digitalProducts")
        {
        }

        public async Task<IList<DigitalProduct>> GetPublicCatalogAsync(int skip, int take)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 500);
            return (await GetItemsAsync(x => x.IsActive))
                .OrderByDescending(x => x.CreationDateTime)
                .Skip(safeSkip)
                .Take(safeTake)
                .ToList();
        }

        public async Task<IList<DigitalProduct>> GetByCategoryIdAsync(string categoryId, int limit = 500)
        {
            var safeLimit = Math.Clamp(limit, 1, 2000);
            return (await GetItemsAsync(x => x.CategoryIds.Contains(categoryId)))
                .Take(safeLimit)
                .ToList();
        }
    }

    public class DigitalProductCategoryMockRepository : BaseMockRepository<DigitalProductCategory>, IDigitalProductCategoryRepository
    {
        public DigitalProductCategoryMockRepository(IMockScenario scenario) : base(scenario, "digitalProductCategories")
        {
        }

        public async Task<IList<DigitalProductCategory>> GetOrderedAsync(int skip, int take)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 500);
            return (await GetItemsAsync())
                .OrderBy(x => x.DisplayOrder)
                .Skip(safeSkip)
                .Take(safeTake)
                .ToList();
        }
    }

    public class CustomerMockRepository : BaseMockRepository<Customer>, ICustomerRepository
    {
        public CustomerMockRepository(IMockScenario scenario) : base(scenario, "customers")
        {
        }
    }

    public class CartMockRepository : BaseMockRepository<Cart>, ICartRepository
    {
        public CartMockRepository(IMockScenario scenario) : base(scenario, "carts")
        {
        }
    }

    public class InsightTopicMockRepository : BaseMockRepository<InsightTopic>, IInsightTopicRepository
    {
        public InsightTopicMockRepository(IMockScenario scenario) : base(scenario, "insightTopics")
        {
        }
    }

    public class InsightNewsItemMockRepository : BaseMockRepository<InsightNewsItem>, IInsightNewsItemRepository
    {
        public InsightNewsItemMockRepository(IMockScenario scenario) : base(scenario, "insightNewsItems")
        {
        }
    }

    public class InsightContentPlanMockRepository : BaseMockRepository<InsightContentPlan>, IInsightContentPlanRepository
    {
        public InsightContentPlanMockRepository(IMockScenario scenario) : base(scenario, "insightContentPlans")
        {
        }
    }

    public class InsightSourceCursorMockRepository : BaseMockRepository<InsightSourceCursor>, IInsightSourceCursorRepository
    {
        public InsightSourceCursorMockRepository(IMockScenario scenario) : base(scenario, "insightSourceCursors")
        {
        }
    }

    public class CompetitionMockRepository : BaseMockRepository<Competition>, ICompetitionRepository
    {
        public CompetitionMockRepository(IMockScenario scenario) : base(scenario, "competitions")
        {
        }
    }

    public class UserChannelMockRepository : BaseMockRepository<UserChannel>, IUserChannelRepository
    {
        public UserChannelMockRepository(IMockScenario scenario) : base(scenario, "userChannels") { }

        public async Task<IList<UserChannel>> GetByUserIdAsync(string userId)
            => (await GetItemsAsync(uc => uc.UserId == userId && uc.IsActive));

        public async Task<IList<UserChannel>> GetByChannelIdAsync(string channelId)
            => (await GetItemsAsync(uc => uc.ChannelId == channelId && uc.IsActive));

        public async Task<UserChannel?> GetByUserAndChannelAsync(string userId, string channelId)
        {
            var all = await GetItemsAsync(uc => uc.UserId == userId && uc.ChannelId == channelId);
            return all.FirstOrDefault();
        }
    }

    public class UserChannelOwnerMockRepository : BaseMockRepository<UserChannelOwner>, IUserChannelOwnerRepository
    {
        public UserChannelOwnerMockRepository(IMockScenario scenario) : base(scenario, "userChannelOwners") { }

        public async Task<IList<UserChannelOwner>> GetByUserIdAsync(string userId)
            => (await GetItemsAsync(o => o.UserId == userId && o.IsActive));

        public async Task<IList<UserChannelOwner>> GetByChannelIdAsync(string channelId)
            => (await GetItemsAsync(o => o.ChannelId == channelId && o.IsActive));
    }

    public class UserRequestMockRepository : BaseMockRepository<UserRequest>, IUserRequestRepository
    {
        public UserRequestMockRepository(IMockScenario scenario) : base(scenario, "userRequests") { }
    }
}


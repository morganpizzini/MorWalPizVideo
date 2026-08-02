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
    }
    public class PageMockRepository : BaseMockRepository<Page>, IPageRepository
    {
        public PageMockRepository(IMockScenario scenario) : base(scenario, "pages")
        {
        }
    }
    public class ProductMockRepository : BaseMockRepository<Product>, IProductRepository
    {
        public ProductMockRepository(IMockScenario scenario) : base(scenario, "products")
        {
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
    }

    public class CompilationMockRepository : BaseMockRepository<Compilation>, ICompilationRepository
    {
        public CompilationMockRepository(IMockScenario scenario) : base(scenario, "compilations")
        {
        }
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
    }

    public class DigitalProductMockRepository : BaseMockRepository<DigitalProduct>, IDigitalProductRepository
    {
        public DigitalProductMockRepository(IMockScenario scenario) : base(scenario, "digitalProducts")
        {
        }
    }

    public class DigitalProductCategoryMockRepository : BaseMockRepository<DigitalProductCategory>, IDigitalProductCategoryRepository
    {
        public DigitalProductCategoryMockRepository(IMockScenario scenario) : base(scenario, "digitalProductCategories")
        {
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


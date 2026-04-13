using Microsoft.Extensions.Hosting;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using System.Security.Cryptography;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public class MatchMockRepository : BaseMockRepository<YouTubeContent>, IYouTubeContentRepository
    {
        public MatchMockRepository(IHostEnvironment environment) : base(environment, "matches")
        {
        }
    }
    public class PageMockRepository : BaseMockRepository<Page>, IPageRepository
    {
        public PageMockRepository(IHostEnvironment environment) : base(environment, "pages")
        {
        }
    }
    public class ProductMockRepository : BaseMockRepository<Product>, IProductRepository
    {
        public ProductMockRepository(IHostEnvironment environment) : base(environment, "products")
        {
        }
    }

    public class ProductCategoryMockRepository : BaseMockRepository<ProductCategory>, IProductCategoryRepository
    {
        public ProductCategoryMockRepository(IHostEnvironment environment) : base(environment, "productCategories")
        {
        }
    }

    public class ConfigurationMockRepository : BaseMockRepository<MorWalPizConfiguration>, IConfigurationRepository
    {
        public ConfigurationMockRepository(IHostEnvironment environment) : base(environment, "configurations")
        {
        }
    }

    public class SponsorMockRepository : BaseMockRepository<Sponsor>, ISponsorRepository
    {
        public SponsorMockRepository(IHostEnvironment environment) : base(environment, "sponsors")
        {
        }
    }
    public class YTChannelMockRepository : BaseMockRepository<YTChannel>, IYTChannelRepository
    {
        public YTChannelMockRepository(IHostEnvironment environment) : base(environment, "ytchannels")
        {
        }
    }
    public class CalendarEventMockRepository : BaseMockRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventMockRepository(IHostEnvironment environment) : base(environment, "calendarEvents")
        {
        }
    }

    public class CompilationMockRepository : BaseMockRepository<Compilation>, ICompilationRepository
    {
        public CompilationMockRepository(IHostEnvironment environment) : base(environment, "compilations")
        {
        }
    }

    public class BioLinkMockRepository : BaseMockRepository<BioLink>, IBioLinkRepository
    {
        public BioLinkMockRepository(IHostEnvironment environment) : base(environment, "bioLinks") { }
    }

    public class SponsorApplyMockRepository : BaseMockRepository<SponsorApply>, ISponsorApplyRepository
    {
        public SponsorApplyMockRepository(IHostEnvironment environment) : base(environment, "sponsorApplies") { }
    }

    public class ShortLinkMockRepository : BaseMockRepository<ShortLink>, IShortLinkRepository
    {
        public ShortLinkMockRepository(IHostEnvironment environment) : base(environment, "shortLinks")
        {
        }
    }

    public class CategoryMockRepository : BaseMockRepository<Category>, ICategoryRepository
    {
        public CategoryMockRepository(IHostEnvironment environment) : base(environment, "categories")
        {
        }
    }
    public class QueryLinkMockRepository : BaseMockRepository<QueryLink>, IQueryLinkRepository
    {
        public QueryLinkMockRepository(IHostEnvironment environment) : base(environment, "queryLinks")
        {
        }
    }

    public class UserMockRepository : BaseMockRepository<User>, IUserRepository
    {
        public UserMockRepository(IHostEnvironment environment) : base(environment, "users")
        {
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var u = username.ToLower();
            return (await this.GetItemsAsync(x=> x.Username.ToLower() == u || x.Email.ToLower() == u)).FirstOrDefault();
        }
    }
    public class LoginAttemptMockRepository : BaseMockRepository<LoginAttempt>, ILoginAttemptRepository
    {
        public LoginAttemptMockRepository(IHostEnvironment environment) : base(environment, "loginAttempts")
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
        public PublishScheduleMockRepository(IHostEnvironment environment) : base(environment, "publishSchedules")
        {
        }
    }

    public class CustomFormMockRepository : BaseMockRepository<CustomForm>, ICustomFormRepository
    {
        public CustomFormMockRepository(IHostEnvironment environment) : base(environment, "customForms")
        {
        }
    }

    public class DigitalProductMockRepository : BaseMockRepository<DigitalProduct>, IDigitalProductRepository
    {
        public DigitalProductMockRepository(IHostEnvironment environment) : base(environment, "digitalProducts")
        {
        }
    }

    public class DigitalProductCategoryMockRepository : BaseMockRepository<DigitalProductCategory>, IDigitalProductCategoryRepository
    {
        public DigitalProductCategoryMockRepository(IHostEnvironment environment) : base(environment, "digitalProductCategories")
        {
        }
    }

    public class CustomerMockRepository : BaseMockRepository<Customer>, ICustomerRepository
    {
        public CustomerMockRepository(IHostEnvironment environment) : base(environment, "customers")
        {
        }
    }

    public class CartMockRepository : BaseMockRepository<Cart>, ICartRepository
    {
        public CartMockRepository(IHostEnvironment environment) : base(environment, "carts")
        {
        }
    }

    public class InsightTopicMockRepository : BaseMockRepository<InsightTopic>, IInsightTopicRepository
    {
        public InsightTopicMockRepository(IHostEnvironment environment) : base(environment, "insightTopics")
        {
        }
    }

    public class InsightNewsItemMockRepository : BaseMockRepository<InsightNewsItem>, IInsightNewsItemRepository
    {
        public InsightNewsItemMockRepository(IHostEnvironment environment) : base(environment, "insightNewsItems")
        {
        }
    }

    public class InsightContentPlanMockRepository : BaseMockRepository<InsightContentPlan>, IInsightContentPlanRepository
    {
        public InsightContentPlanMockRepository(IHostEnvironment environment) : base(environment, "insightContentPlans")
        {
        }
    }
}

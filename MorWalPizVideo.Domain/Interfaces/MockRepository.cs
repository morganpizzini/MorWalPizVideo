using Microsoft.Extensions.Hosting;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public class MatchMockRepository : BaseMockRepository<Match>, IMatchRepository
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
    public class PublishScheduleMockRepository : BaseMockRepository<PublishSchedule>, IPublishScheduleRepository
    {
        public PublishScheduleMockRepository(IHostEnvironment environment) : base(environment, "publishSchedules")
        {
        }
    }
}

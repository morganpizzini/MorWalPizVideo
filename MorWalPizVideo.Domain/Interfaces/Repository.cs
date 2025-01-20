using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public class MatchRepository : BaseRepository<Match>, IMatchRepository
    {
        public MatchRepository(IMongoDatabase database) : base(database, DbCollections.Matches)
        {
        }
    }
    public class PageRepository : BaseRepository<Page>, IPageRepository
    {
        public PageRepository(IMongoDatabase database) : base(database, DbCollections.Pages)
        {
        }
    }
    public class SponsorRepository : BaseRepository<Sponsor>, ISponsorRepository
    {
        public SponsorRepository(IMongoDatabase database) : base(database, DbCollections.Sponsors)
        {
        }
    }
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(IMongoDatabase database) : base(database, DbCollections.Products)
        {
        }
    }
    public class CalendarEventRepository : BaseRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventRepository(IMongoDatabase database) : base(database, DbCollections.CalendarEvents)
        {
        }
    }

    public class ShortLinkRepository : BaseRepository<ShortLink>, IShortLinkRepository
    {
        public ShortLinkRepository(IMongoDatabase database) : base(database, DbCollections.ShortLinks)
        {
        }
    }
    public class YTChannelRepository : BaseRepository<YTChannel>, IYTChannelRepository
    {
        public YTChannelRepository(IMongoDatabase database) : base(database, DbCollections.Channels)
        {
        }
    }

    public class SponsorApplyRepository : BaseRepository<SponsorApply>, ISponsorApplyRepository
    {
        public SponsorApplyRepository(IMongoDatabase database) : base(database, DbCollections.SponsorApplies)
        {
        }
    }
    public class BioLinkRepository : BaseRepository<BioLink>, IBioLinkRepository
    {
        public BioLinkRepository(IMongoDatabase database) : base(database, DbCollections.BioLinks)
        {
        }
    }
}

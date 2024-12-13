using MongoDB.Driver;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public class MatchRepository : BaseRepository<Match>, IMatchRepository
    {
        public MatchRepository(IMongoDatabase database) : base(database, "matches")
        {
        }
    }
    public class PageRepository : BaseRepository<Page>, IPageRepository
    {
        public PageRepository(IMongoDatabase database) : base(database, "pages")
        {
        }
    }
    public class SponsorRepository : BaseRepository<Sponsor>, ISponsorRepository
    {
        public SponsorRepository(IMongoDatabase database) : base(database, "sponsors")
        {
        }
    }
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(IMongoDatabase database) : base(database, "products")
        {
        }
    }
    public class CalendarEventRepository : BaseRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventRepository(IMongoDatabase database) : base(database, "calendarEvents")
        {
        }
    }
    public class BioLinkRepository : BaseRepository<BioLink>, IBioLinkRepository
    {
        public BioLinkRepository(IMongoDatabase database) : base(database, "bioLinks")
        {
        }
    }
}

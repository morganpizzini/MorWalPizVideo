using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services
{
    public class DataService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IMatchRepository _matchRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISponsorRepository _sponsorRepository;
        private readonly IPageRepository _pageRepository;
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IShortLinkRepository _shortLinkRepository;
        public DataService(IWebHostEnvironment environment, IMatchRepository matchRepository, IProductRepository productRepository, ISponsorRepository sponsorRepository, IPageRepository pageRepository, ICalendarEventRepository calendarEventRepository, IShortLinkRepository shortLinkRepository)
        {
            _environment = environment;
            _matchRepository = matchRepository;
            _productRepository = productRepository;
            _sponsorRepository = sponsorRepository;
            _pageRepository = pageRepository;
            _calendarEventRepository = calendarEventRepository;
            _shortLinkRepository = shortLinkRepository;
        }

        public Task<IList<ShortLink>> FetchShortLinks() => _shortLinkRepository.GetItemsAsync();
        public async Task<ShortLink?> GetShortLink(string shortLink) => (await _shortLinkRepository.GetItemsAsync(x=>x.Code == shortLink)).FirstOrDefault();
        public Task UpdateShortlink(ShortLink entity) => _shortLinkRepository.UpdateItemAsync(entity);
        public Task<IList<Match>> GetItems() => _matchRepository.GetItemsAsync();
        public Task<IList<Product>> GetProducts() => _productRepository.GetItemsAsync();

        public Task<IList<Sponsor>> GetSponsors() => _sponsorRepository.GetItemsAsync();
        public Task<IList<Page>> GetPages() => _pageRepository.GetItemsAsync();
        public Task<IList<CalendarEvent>> GetCalendarEvents() => _calendarEventRepository.GetItemsAsync();

    }
}

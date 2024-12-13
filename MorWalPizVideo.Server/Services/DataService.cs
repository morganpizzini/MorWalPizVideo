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
        private readonly IBioLinkRepository _bioLinkRepository;
        public DataService(IWebHostEnvironment environment, IMatchRepository matchRepository, IProductRepository productRepository, ISponsorRepository sponsorRepository, IPageRepository pageRepository, ICalendarEventRepository calendarEventRepository, IBioLinkRepository bioLinkRepository)
        {
            _environment = environment;
            _matchRepository = matchRepository;
            _productRepository = productRepository;
            _sponsorRepository = sponsorRepository;
            _pageRepository = pageRepository;
            _calendarEventRepository = calendarEventRepository;
            _bioLinkRepository = bioLinkRepository;
        }

        public Task<IList<Match>> GetItems() => _matchRepository.GetItemsAsync();
       
        public Task<IList<Product>> GetProducts() => _productRepository.GetItemsAsync();

        public Task<IList<Sponsor>> GetSponsors() => _sponsorRepository.GetItemsAsync();
        public Task<IList<Page>> GetPages() => _pageRepository.GetItemsAsync();
        public Task<IList<CalendarEvent>> GetCalendarEvents() => _calendarEventRepository.GetItemsAsync();
        public Task<IList<BioLink>> GetBioLinks() => _bioLinkRepository.GetItemsAsync();
    }
}

using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services
{
    public class DataService
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISponsorRepository _sponsorRepository;
        private readonly ISponsorApplyRepository _sponsorApplyRepository;
        private readonly IPageRepository _pageRepository;
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IBioLinkRepository _bioLinkRepository;
        private readonly IShortLinkRepository _shortLinkRepository;
        public DataService(IMatchRepository matchRepository, ISponsorApplyRepository sponsorApplyRepository, IProductRepository productRepository, ISponsorRepository sponsorRepository, IPageRepository pageRepository, ICalendarEventRepository calendarEventRepository, IBioLinkRepository bioLinkRepository, IShortLinkRepository shortLinkRepository)
        {
            _matchRepository = matchRepository;
            _productRepository = productRepository;
            _sponsorRepository = sponsorRepository;
            _pageRepository = pageRepository;
            _calendarEventRepository = calendarEventRepository;
            _bioLinkRepository = bioLinkRepository;
            _shortLinkRepository = shortLinkRepository;
            _sponsorApplyRepository = sponsorApplyRepository;
        }

        public Task<IList<ShortLink>> FetchShortLinks() => _shortLinkRepository.GetItemsAsync();
        public async Task<ShortLink?> GetShortLink(string shortLink) => (await _shortLinkRepository.GetItemsAsync(x => x.Code == shortLink)).FirstOrDefault();
        public Task UpdateShortlink(ShortLink entity) => _shortLinkRepository.UpdateItemAsync(entity);
        public Task<IList<Match>> GetItems() => _matchRepository.GetItemsAsync();
        public Task<IList<Product>> GetProducts() => _productRepository.GetItemsAsync();
        public Task<IList<Sponsor>> GetSponsors() => _sponsorRepository.GetItemsAsync();
        public async Task SaveSponsorApplies(SponsorApply entity)
        {
            var checkSponsors = await _sponsorApplyRepository.GetItemsAsync(x => x.Email == entity.Email);
            if (checkSponsors.Count > 0)
                return;
            await _sponsorApplyRepository.AddItemAsync(entity);
        }
        public Task<IList<Page>> GetPages() => _pageRepository.GetItemsAsync();
        public Task<IList<CalendarEvent>> GetCalendarEvents() => _calendarEventRepository.GetItemsAsync();
        public async Task<IList<BioLink>> GetBioLinks() => [.. (await _bioLinkRepository.GetItemsAsync(x => x.Enable)).OrderBy(x => x.Order)];
    }
}

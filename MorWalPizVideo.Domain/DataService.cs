using Google.Apis.YouTube.v3.Data;
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
        private readonly IYTChannelRepository _ytChannelRepository;
        public DataService(IMatchRepository matchRepository, ISponsorApplyRepository sponsorApplyRepository, IProductRepository productRepository, ISponsorRepository sponsorRepository, IPageRepository pageRepository, ICalendarEventRepository calendarEventRepository, IBioLinkRepository bioLinkRepository, IShortLinkRepository shortLinkRepository, IYTChannelRepository ytChannelRepository)
        {
            _matchRepository = matchRepository;
            _productRepository = productRepository;
            _sponsorRepository = sponsorRepository;
            _pageRepository = pageRepository;
            _calendarEventRepository = calendarEventRepository;
            _bioLinkRepository = bioLinkRepository;
            _shortLinkRepository = shortLinkRepository;
            _sponsorApplyRepository = sponsorApplyRepository;
            _ytChannelRepository = ytChannelRepository;
        }

        public Task<IList<ShortLink>> FetchShortLinks() => _shortLinkRepository.GetItemsAsync();
        public async Task<ShortLink?> GetShortLink(string shortLink) => (await _shortLinkRepository.GetItemsAsync(x => x.Code.ToLower() == shortLink.ToLower())).FirstOrDefault();
        public Task UpdateShortlink(ShortLink entity) => _shortLinkRepository.UpdateItemAsync(entity);
        public Task<IList<Match>> GetMatches() => _matchRepository.GetItemsAsync();
        public async Task<Match?> FindMatch(string matchId) => (await _matchRepository.GetItemsAsync(x => x.ThumbnailUrl == matchId)).FirstOrDefault();
        public async Task SaveMatch(Match entity)
        {
            var check = await _matchRepository.GetItemsAsync(x => x.ThumbnailUrl == entity.MatchId);
            if (check.Count > 0)
                return;
            await _matchRepository.AddItemAsync(entity);
        }
        public async Task UpdateMatch(Match entity)
        {
            var check = await _matchRepository.GetItemsAsync(x => x.ThumbnailUrl == entity.MatchId);
            if (check.Count == 0)
                return;
            await _matchRepository.UpdateItemAsync(entity);
        }
        public Task<IList<YTChannel>> GetChannels() => _ytChannelRepository.GetItemsAsync();
        public async Task<YTChannel?> GetChannel(string channelName) => (await _ytChannelRepository.GetItemsAsync(x => x.ChannelName == channelName)).FirstOrDefault();
        public async Task SaveChannel(YTChannel entity)
        {
            var checkSponsors = await _ytChannelRepository.GetItemsAsync(x => x.ChannelId == entity.ChannelId);
            if (checkSponsors.Count > 0)
                return;
            await _ytChannelRepository.AddItemAsync(entity);
        }
        public async Task RemoveChannel(string channelName)
        {
            var channel = (await _ytChannelRepository.GetItemsAsync(x=>x.ChannelName == channelName)).FirstOrDefault();
            if (channel == null)
            {
                return;
            }
            await _ytChannelRepository.DeleteItemAsync(channel.Id);
        }

        public Task<IList<Product>> GetProducts() => _productRepository.GetItemsAsync();
        public Task<IList<Sponsor>> GetSponsors() => _sponsorRepository.GetItemsAsync();
        public Task<IList<SponsorApply>> GetSponsorApplies() => _sponsorApplyRepository.GetItemsAsync();
        public async Task SaveSponsorApplies(SponsorApply entity)
        {
            var checkSponsors = await _sponsorApplyRepository.GetItemsAsync(x => x.Email == entity.Email);
            if (checkSponsors.Count > 0)
                return;
            await _sponsorApplyRepository.AddItemAsync(entity);
        }
        public Task<IList<Page>> GetPages() => _pageRepository.GetItemsAsync();
        public async Task SavePage(Page entity)
        {
            var curentPage = await _pageRepository.GetItemsAsync(x => x.Url == entity.Url);
            if (curentPage.Count > 0)
                return;
            await _pageRepository.AddItemAsync(entity);
        }
        public async Task RemovePage(string pageId)
        {
            var page = (await _pageRepository.GetItemsAsync(x => x.Id == pageId)).FirstOrDefault();
            if (page == null)
            {
                return;
            }
            await _pageRepository.DeleteItemAsync(page.Id);
        }
        public Task<IList<CalendarEvent>> GetCalendarEvents() => _calendarEventRepository.GetItemsAsync();
        public async Task<IList<BioLink>> GetBioLinks() => [.. (await _bioLinkRepository.GetItemsAsync(x => x.Enable)).OrderBy(x => x.Order)];
    }
}

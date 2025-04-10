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
        private readonly ICategoryRepository _categoryRepository;
        private readonly IQueryLinkRepository _queryLinkRepository;

        public DataService(
            IMatchRepository matchRepository,
            ISponsorApplyRepository sponsorApplyRepository,
            IProductRepository productRepository,
            ISponsorRepository sponsorRepository,
            IPageRepository pageRepository,
            ICalendarEventRepository calendarEventRepository,
            IBioLinkRepository bioLinkRepository,
            IShortLinkRepository shortLinkRepository,
            IYTChannelRepository ytChannelRepository,
            ICategoryRepository categoryRepository,
            IQueryLinkRepository queryLinkRepository)
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
            _categoryRepository = categoryRepository;
            _queryLinkRepository = queryLinkRepository;
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
            var channel = (await _ytChannelRepository.GetItemsAsync(x => x.ChannelName == channelName)).FirstOrDefault();
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

        // Category methods
        public Task<IList<Category>> GetCategories() => _categoryRepository.GetItemsAsync();

        public async Task<Category?> GetCategory(string title) =>
            (await _categoryRepository.GetItemsAsync(x => x.Title.ToLower() == title.ToLower())).FirstOrDefault();

        public async Task<Category?> GetCategoryById(string id) =>
            await _categoryRepository.GetItemAsync(id);

        public async Task SaveCategory(Category entity)
        {
            var existingCategory = await _categoryRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingCategory.Count > 0)
                return;

            await _categoryRepository.AddItemAsync(entity);
        }

        public async Task UpdateCategory(Category entity)
        {
            var existingCategory = await _categoryRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingCategory.Count == 0)
                return;

            await _categoryRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteCategory(string categoryId)
        {
            var category = (await _categoryRepository.GetItemsAsync(x => x.Id == categoryId)).FirstOrDefault();
            if (category == null)
                return;

            await _categoryRepository.DeleteItemAsync(category.Id);
        }

        public async Task<CalendarEvent?> GetCalendarEventByTitle(string title) =>
            (await _calendarEventRepository.GetItemsAsync(x => x.Title.ToLower() == title.ToLower())).FirstOrDefault();

        public async Task SaveCalendarEvent(CalendarEvent entity)
        {
            var existingEvent = await _calendarEventRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingEvent.Count > 0)
                return;

            await _calendarEventRepository.AddItemAsync(entity);
        }

        public async Task UpdateCalendarEvent(CalendarEvent entity)
        {
            var existingEvent = await _calendarEventRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingEvent.Count == 0)
                return;

            await _calendarEventRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteCalendarEvent(string calendarEventId)
        {
            await _calendarEventRepository.DeleteItemAsync(calendarEventId);
        }

        // QueryLink methods
        public Task<IList<QueryLink>> GetQueryLinks() => _queryLinkRepository.GetItemsAsync();

        public async Task<QueryLink?> GetQueryLinkByTitle(string title) =>
            (await _queryLinkRepository.GetItemsAsync(x => x.Title.ToLower() == title.ToLower())).FirstOrDefault();

        public async Task SaveQueryLink(QueryLink entity)
        {
            var existingLink = await _queryLinkRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingLink.Count > 0)
                return;

            await _queryLinkRepository.AddItemAsync(entity);
        }

        public async Task UpdateQueryLink(QueryLink entity)
        {
            var existingLink = await _queryLinkRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingLink.Count == 0)
                return;

            await _queryLinkRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteQueryLink(string queryLinkId)
        {
            await _queryLinkRepository.DeleteItemAsync(queryLinkId);
        }
    }
}

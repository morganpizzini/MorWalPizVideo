using Google.Apis.YouTube.v3.Data;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using System.Threading.Channels;

namespace MorWalPizVideo.Server.Services
{
    public interface IGenericDataService
    {
        Task<IList<YouTubeContent>> FetchMatches();
        Task UpdateMatch(YouTubeContent entity);
        Task<IList<Compilation>> GetCompilations();
        Task<CustomForm?> GetCustomFormById(string id);
        Task AddFormResponse(string formId, CustomFormResponse response);
        Task<IList<BioLink>> GetBioLinks();
        Task<IList<CalendarEvent>> GetCalendarEvents();
        Task<IList<MorWalPizConfiguration>> FetchConfigurationByKeys(IList<string> keys);
        Task<IList<CustomForm>> GetActiveForms();
        Task<CustomForm?> GetCustomFormByUrl(string url);
        Task<IList<Page>> GetPages();
        Task<IList<Product>> GetProducts();
        Task<IList<Sponsor>> GetSponsors();
        Task SaveSponsorApplies(SponsorApply entity);
    }
    public class MinimalDataService : IGenericDataService {
        protected readonly IYouTubeContentRepository _youTubeContent;
        protected readonly ICompilationRepository _compilationRepository;
        protected readonly ICustomFormRepository _customFormRepository;
        protected readonly ICalendarEventRepository _calendarEventRepository;
        protected readonly IBioLinkRepository _bioLinkRepository;
        protected readonly IConfigurationRepository _configurationRepository;
        protected readonly IPageRepository _pageRepository;
        protected readonly IProductRepository _productRepository;
        protected readonly ISponsorRepository _sponsorRepository;
        protected readonly ISponsorApplyRepository _sponsorApplyRepository;
        public MinimalDataService(IYouTubeContentRepository youTubeContent, ICompilationRepository compilationRepository, ICustomFormRepository customFormRepository, ICalendarEventRepository calendarEventRepository, IBioLinkRepository bioLinkRepository, IConfigurationRepository configurationRepository, IPageRepository pageRepository, IProductRepository productRepository, ISponsorRepository sponsorRepository, ISponsorApplyRepository sponsorApplyRepository)
        {
            _youTubeContent = youTubeContent;
            _compilationRepository = compilationRepository;
            _customFormRepository = customFormRepository;
            _calendarEventRepository = calendarEventRepository;
            _bioLinkRepository = bioLinkRepository;
            _configurationRepository = configurationRepository;
            _pageRepository = pageRepository;
            _productRepository = productRepository;
            _bioLinkRepository = bioLinkRepository;
            _configurationRepository = configurationRepository;
            _sponsorRepository = sponsorRepository;
            _sponsorApplyRepository = sponsorApplyRepository;
        }
        public async Task<IList<BioLink>> GetBioLinks() => [.. (await _bioLinkRepository.GetItemsAsync(x => x.Enable)).OrderBy(x => x.Order)];
        public Task<IList<CalendarEvent>> GetCalendarEvents() => _calendarEventRepository.GetItemsAsync();

        public Task<IList<MorWalPizConfiguration>> FetchConfigurationByKeys(IList<string> keys)
        {
            var k = keys.Select(x => x.ToLower());
            return _configurationRepository.GetItemsAsync(x => k.Contains(x.Key.ToLower()));
        }
        public async Task<IList<YouTubeContent>> FetchMatches() => [.. (await _youTubeContent.GetItemsAsync()).OrderByDescending(x => x.CreationDateTime)];
        public async Task UpdateMatch(YouTubeContent entity)
        {
            // Check if a match with the same ID exists
            var check = await _youTubeContent.GetItemsAsync(x => x.Id == entity.Id);

            if (check.Count == 0)
                // Try fallback to ThumbnailVideoId for backward compatibility
                check = await _youTubeContent.GetItemsAsync(x => x.ThumbnailVideoId == entity.ThumbnailVideoId);

            if (check.Count == 0)
                return;

            await _youTubeContent.UpdateItemAsync(entity);
        }

        public Task<IList<Compilation>> GetCompilations() => _compilationRepository.GetItemsAsync();
        public async Task<CustomForm?> GetCustomFormById(string id) =>
            await _customFormRepository.GetItemAsync(id);
        public async Task AddFormResponse(string formId, CustomFormResponse response)
        {
            var form = await GetCustomFormById(formId);
            if (form == null)
                return;

            var updatedForm = form.AddResponse(response);
            await _customFormRepository.UpdateItemAsync(updatedForm);
        }
        public async Task<IList<CustomForm>> GetActiveForms() =>
            await _customFormRepository.GetItemsAsync(x => x.Active);
        public async Task<CustomForm?> GetCustomFormByUrl(string url) =>
            (await _customFormRepository.GetItemsAsync(x => x.Url.ToLower() == url.ToLower())).FirstOrDefault();
        public Task<IList<Page>> GetPages() => _pageRepository.GetItemsAsync();

        public Task<IList<Product>> GetProducts() => _productRepository.GetItemsAsync();
        public Task<IList<Sponsor>> GetSponsors() => _sponsorRepository.GetItemsAsync();
        public async Task SaveSponsorApplies(SponsorApply entity)
        {
            var checkSponsors = await _sponsorApplyRepository.GetItemsAsync(x => x.Email == entity.Email);
            if (checkSponsors.Count > 0)
                return;
            await _sponsorApplyRepository.AddItemAsync(entity);
        }

    }
    public class DataService : MinimalDataService
    {
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly IShortLinkRepository _shortLinkRepository;
        private readonly IYTChannelRepository _ytChannelRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IQueryLinkRepository _queryLinkRepository;
        private readonly IPublishScheduleRepository _publishScheduleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDigitalProductRepository _digitalProductRepository;
        private readonly IDigitalProductCategoryRepository _digitalProductCategoryRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IInsightTopicRepository _insightTopicRepository;
        private readonly IInsightNewsItemRepository _insightNewsItemRepository;
        private readonly IInsightContentPlanRepository _insightContentPlanRepository;
        public DataService(
            IYouTubeContentRepository youTubeContent,
            ISponsorApplyRepository sponsorApplyRepository,
            IProductRepository productRepository,
            IProductCategoryRepository productCategoryRepository,
            ISponsorRepository sponsorRepository,
            IPageRepository pageRepository,
            ICalendarEventRepository calendarEventRepository,
            ICompilationRepository compilationRepository,
            IBioLinkRepository bioLinkRepository,
            IShortLinkRepository shortLinkRepository,
            IYTChannelRepository ytChannelRepository,
            ICategoryRepository categoryRepository,
            IUserRepository userRepository,
            IQueryLinkRepository queryLinkRepository,
            IPublishScheduleRepository publishScheduleRepository,
            IConfigurationRepository configurationRepository,
            ICustomFormRepository customFormRepository,
            IDigitalProductRepository digitalProductRepository,
            IDigitalProductCategoryRepository digitalProductCategoryRepository,
            ICustomerRepository customerRepository,
            ICartRepository cartRepository,
            IInsightTopicRepository insightTopicRepository,
            IInsightNewsItemRepository insightNewsItemRepository,
            IInsightContentPlanRepository insightContentPlanRepository): base(youTubeContent, compilationRepository, customFormRepository, calendarEventRepository, bioLinkRepository, configurationRepository, pageRepository, productRepository,sponsorRepository,sponsorApplyRepository)
        {
            _productCategoryRepository = productCategoryRepository;
            _shortLinkRepository = shortLinkRepository;
            _ytChannelRepository = ytChannelRepository;
            _categoryRepository = categoryRepository;
            _queryLinkRepository = queryLinkRepository;
            _publishScheduleRepository = publishScheduleRepository;
            _userRepository = userRepository;
            _digitalProductRepository = digitalProductRepository;
            _digitalProductCategoryRepository = digitalProductCategoryRepository;
            _customerRepository = customerRepository;
            _cartRepository = cartRepository;
            _insightTopicRepository = insightTopicRepository;
            _insightNewsItemRepository = insightNewsItemRepository;
            _insightContentPlanRepository = insightContentPlanRepository;
        }

        // Shop - DigitalProduct methods
        public Task<IList<DigitalProduct>> GetDigitalProductsAsync() =>
            _digitalProductRepository.GetItemsAsync(x => x.IsActive);

        public async Task<DigitalProduct?> GetDigitalProductByIdAsync(string id) =>
            await _digitalProductRepository.GetItemAsync(id);

        public async Task SaveDigitalProduct(DigitalProduct entity)
        {
            var existingProduct = await _digitalProductRepository.GetItemsAsync(x => x.Name.ToLower() == entity.Name.ToLower());
            if (existingProduct.Count > 0)
                return;

            await _digitalProductRepository.AddItemAsync(entity);
        }

        public async Task UpdateDigitalProduct(DigitalProduct entity)
        {
            var existingProduct = await _digitalProductRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingProduct.Count == 0)
                return;

            await _digitalProductRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteDigitalProduct(string productId)
        {
            var product = (await _digitalProductRepository.GetItemsAsync(x => x.Id == productId)).FirstOrDefault();
            if (product == null)
                return;

            await _digitalProductRepository.DeleteItemAsync(product.Id);
        }

        // Shop - DigitalProductCategory methods
        public async Task<IList<DigitalProductCategory>> GetProductCategoriesAsync() =>
            await _digitalProductCategoryRepository.GetItemsAsync();

        public async Task<DigitalProductCategory?> GetDigitalProductCategoryByIdAsync(string id) =>
            await _digitalProductCategoryRepository.GetItemAsync(id);

        public async Task SaveDigitalProductCategory(DigitalProductCategory entity)
        {
            var existingCategory = await _digitalProductCategoryRepository.GetItemsAsync(x => x.Name.ToLower() == entity.Name.ToLower());
            if (existingCategory.Count > 0)
                return;

            await _digitalProductCategoryRepository.AddItemAsync(entity);
        }

        public async Task UpdateDigitalProductCategory(DigitalProductCategory entity)
        {
            var existingCategory = await _digitalProductCategoryRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingCategory.Count == 0)
                return;

            await _digitalProductCategoryRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteDigitalProductCategory(string categoryId)
        {
            var category = (await _digitalProductCategoryRepository.GetItemsAsync(x => x.Id == categoryId)).FirstOrDefault();
            if (category == null)
                return;

            await _digitalProductCategoryRepository.DeleteItemAsync(category.Id);
        }

        // Shop - Customer methods
        public Task<IList<Customer>> GetCustomersAsync() =>
            _customerRepository.GetItemsAsync();

        public async Task<Customer?> GetCustomerByIdAsync(string id) =>
            await _customerRepository.GetItemAsync(id);

        public async Task<Customer?> GetCustomerByEmailAsync(string email) =>
            (await _customerRepository.GetItemsAsync(x => x.Email.ToLower() == email.ToLower())).FirstOrDefault();

        public async Task SaveCustomerAsync(Customer entity)
        {
            var existingCustomer = await _customerRepository.GetItemsAsync(x => x.Email.ToLower() == entity.Email.ToLower());
            if (existingCustomer.Count > 0)
                return;

            await _customerRepository.AddItemAsync(entity);
        }

        public async Task UpdateCustomerAsync(Customer entity)
        {
            var existingCustomer = await _customerRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingCustomer.Count == 0)
                return;

            await _customerRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteCustomer(string customerId)
        {
            var customer = (await _customerRepository.GetItemsAsync(x => x.Id == customerId)).FirstOrDefault();
            if (customer == null)
                return;

            await _customerRepository.DeleteItemAsync(customer.Id);
        }

        // Shop - Cart methods
        public async Task<Cart?> GetOrCreateCartAsync(string customerId)
        {
            // Try to find an active (non-completed) cart for the customer
            var existingCart = (await _cartRepository.GetItemsAsync(x =>
                x.CustomerId == customerId && !x.IsCompleted)).FirstOrDefault();

            if (existingCart != null)
                return existingCart;

            // Create a new cart
            var newCart = new Cart(
                customerId: customerId,
                items: new List<CartItem>(),
                isCompleted: false,
                completedAt: null
            )
            {
                Id = Guid.NewGuid().ToString()
            };

            await _cartRepository.AddItemAsync(newCart);
            return newCart;
        }

        public async Task<Cart?> GetCartByIdAsync(string cartId) =>
            await _cartRepository.GetItemAsync(cartId);

        public async Task SaveCartAsync(Cart entity)
        {
            var existingCart = await _cartRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingCart.Count > 0)
            {
                // Update existing cart
                await _cartRepository.UpdateItemAsync(entity);
            }
            else
            {
                // Add new cart
                await _cartRepository.AddItemAsync(entity);
            }
        }

        public async Task UpdateCartAsync(Cart entity)
        {
            var existingCart = await _cartRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingCart.Count == 0)
                return;

            await _cartRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteCart(string cartId)
        {
            var cart = (await _cartRepository.GetItemsAsync(x => x.Id == cartId)).FirstOrDefault();
            if (cart == null)
                return;

            await _cartRepository.DeleteItemAsync(cart.Id);
        }

        public Task<IList<ShortLink>> FetchShortLinks() => _shortLinkRepository.GetItemsAsync();
        public async Task<ShortLink?> GetShortLinkByCode(string shortLink) => (await _shortLinkRepository.GetItemsAsync(x => x.Code.ToLower() == shortLink.ToLower())).FirstOrDefault();
        public async Task<ShortLink?> GetShortLink(string id) => (await _shortLinkRepository.GetItemsAsync(x => x.Id.ToLower() == id.ToLower())).FirstOrDefault();
        public Task UpdateShortlink(ShortLink entity) => _shortLinkRepository.UpdateItemAsync(entity);

        public async Task<ShortLink> SaveShortLink(ShortLink entity)
        {
            var existingShortLink = await _shortLinkRepository.GetItemsAsync(x => x.Code.ToLower() == entity.Code.ToLower());
            if (existingShortLink.Count > 0)
                return entity;
            return await _shortLinkRepository.AddItemAsync(entity);
        }

        public async Task DeleteShortLink(string shortLinkId)
        {
            var shortLink = (await _shortLinkRepository.GetItemsAsync(x => x.Id == shortLinkId)).FirstOrDefault();
            if (shortLink == null)
                return;
            await _shortLinkRepository.DeleteItemAsync(shortLink.Id);
        }
        public async Task<YouTubeContent?> GetMatch(string id) => (await _youTubeContent.GetItemsAsync(x => x.Id == id)).FirstOrDefault();
        public async Task<YouTubeContent?> FindMatch(string matchId) =>
            // Try to find by ThumbnailVideoId first (for backward compatibility and for single videos)
            (await _youTubeContent.GetItemsAsync(x => x.ThumbnailVideoId == matchId)).FirstOrDefault() ??
            // Then try to find by Id (for collections)
            (await _youTubeContent.GetItemsAsync(x => x.Id == matchId)).FirstOrDefault();

        public async Task SaveMatch(YouTubeContent entity)
        {
            // Check if a match with the same ID or ThumbnailVideoId already exists
            var check = await _youTubeContent.GetItemsAsync(x =>
                x.Id == entity.Id ||
                x.ThumbnailVideoId == entity.ThumbnailVideoId);

            if (check.Count > 0)
                return;

            await _youTubeContent.AddItemAsync(entity);
        }

        
        public Task<IList<YTChannel>> FetchChannels() => _ytChannelRepository.GetItemsAsync();
        public Task<IList<YTChannel>> GetChannels() => _ytChannelRepository.GetItemsAsync();
        public async Task<YTChannel?> FindChannel(string channelName) =>
            // Try to find by ThumbnailVideoId first (for backward compatibility and for single videos)
            (await _ytChannelRepository.GetItemsAsync(x => x.ChannelName == channelName)).FirstOrDefault() ??
            // Then try to find by Id (for collections)
            (await _ytChannelRepository.GetItemsAsync(x => x.ChannelId == channelName)).FirstOrDefault();
        public async Task<YTChannel?> GetChannel(string channelName) => (await _ytChannelRepository.GetItemsAsync(x => x.ChannelName == channelName)).FirstOrDefault();
        public async Task<YTChannel?> GetChannelById(string channelId) => (await _ytChannelRepository.GetItemsAsync(x => x.ChannelId == channelId)).FirstOrDefault();
        public async Task SaveChannel(YTChannel entity)
        {
            var checkSponsors = await _ytChannelRepository.GetItemsAsync(x => x.ChannelId == entity.ChannelId);
            if (checkSponsors.Count > 0)
                return;
            await _ytChannelRepository.AddItemAsync(entity);
        }
        public async Task UpdateChannel(YTChannel entity)
        {
            var check = await _ytChannelRepository.GetItemsAsync(x => x.ChannelId == entity.ChannelId);
            if (check.Count == 0)
                return;
            await _ytChannelRepository.UpdateItemAsync(entity);
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

        // Product methods
        

        public async Task<Product?> GetProductById(string id) =>
            await _productRepository.GetItemAsync(id);

        public async Task SaveProduct(Product entity)
        {
            var existingProduct = await _productRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingProduct.Count > 0)
                return;

            await _productRepository.AddItemAsync(entity);
        }

        public async Task UpdateProduct(Product entity)
        {
            var existingProduct = await _productRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingProduct.Count == 0)
                return;

            await _productRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteProduct(string productId)
        {
            var product = (await _productRepository.GetItemsAsync(x => x.Id == productId)).FirstOrDefault();
            if (product == null)
                return;

            await _productRepository.DeleteItemAsync(product.Id);
        }

        // ProductCategory methods
        public Task<IList<ProductCategory>> FetchProductCategories(IList<string>? ids = null) =>
            _productCategoryRepository.GetItemsAsync(x => ids != null ? ids.Contains(x.Id) : true);

        public async Task<ProductCategory?> GetProductCategoryById(string id) =>
            await _productCategoryRepository.GetItemAsync(id);

        public async Task SaveProductCategory(ProductCategory entity)
        {
            var existingCategory = await _productCategoryRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingCategory.Count > 0)
                return;

            await _productCategoryRepository.AddItemAsync(entity);
        }

        public async Task UpdateProductCategory(ProductCategory entity)
        {
            var existingCategory = await _productCategoryRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingCategory.Count == 0)
                return;

            await _productCategoryRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteProductCategory(string categoryId)
        {
            var category = (await _productCategoryRepository.GetItemsAsync(x => x.Id == categoryId)).FirstOrDefault();
            if (category == null)
                return;

            await _productCategoryRepository.DeleteItemAsync(category.Id);
        }

        // Sponsor methods
        

        public async Task<Sponsor?> GetSponsorById(string id) =>
            await _sponsorRepository.GetItemAsync(id);

        public async Task SaveSponsor(Sponsor entity)
        {
            var existingSponsor = await _sponsorRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingSponsor.Count > 0)
                return;

            await _sponsorRepository.AddItemAsync(entity);
        }

        public async Task UpdateSponsor(Sponsor entity)
        {
            var existingSponsor = await _sponsorRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingSponsor.Count == 0)
                return;

            await _sponsorRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteSponsor(string sponsorId)
        {
            var sponsor = (await _sponsorRepository.GetItemsAsync(x => x.Id == sponsorId)).FirstOrDefault();
            if (sponsor == null)
                return;

            await _sponsorRepository.DeleteItemAsync(sponsor.Id);
        }
        public Task<IList<SponsorApply>> GetSponsorApplies() => _sponsorApplyRepository.GetItemsAsync();
        
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

        #region Configuration
        public Task<IList<MorWalPizConfiguration>> GetConfigurations() =>
            _configurationRepository.GetItemsAsync();

        public async Task<MorWalPizConfiguration?> GetConfigurationById(string id) =>
            await _configurationRepository.GetItemAsync(id);
        

        public async Task<MorWalPizConfiguration?> GetConfigurationByKey(string key) =>
            (await _configurationRepository.GetItemsAsync(x => x.Key.ToLower() == key.ToLower())).FirstOrDefault();


        public async Task SaveConfiguration(MorWalPizConfiguration configuration)
        {
            var existingConfig = await _configurationRepository.GetItemsAsync(x => x.Key.ToLower() == configuration.Key.ToLower());
            if (existingConfig.Count > 0)
                return;
            await _configurationRepository.AddItemAsync(configuration);
        }

        public async Task UpdateConfiguration(MorWalPizConfiguration configuration)
        {
            var existingConfig = await _configurationRepository.GetItemsAsync(x => x.Id == configuration.Id);
            if (existingConfig.Count == 0)
                return;
            await _configurationRepository.UpdateItemAsync(configuration);
        }

        public async Task DeleteConfiguration(string id)
        {
            var config = await _configurationRepository.GetItemAsync(id);
            if (config == null)
                return;
            await _configurationRepository.DeleteItemAsync(id);
        }
        #endregion

        // publishSchedule methods

        public Task<IList<PublishSchedule>> GetPublishSchedules() => _publishScheduleRepository.GetItemsAsync();

        public async Task<PublishSchedule?> GetPublishScheduleByVideoId(string videoId) =>
            (await _publishScheduleRepository.GetItemsAsync(x => x.VideoId.ToLower() == videoId.ToLower())).FirstOrDefault();

        public async Task<PublishSchedule?> GetPublishScheduleById(string id) =>
            await _publishScheduleRepository.GetItemAsync(id);

        public async Task SavePublishSchedule(PublishSchedule entity)
        {
            var existingPublishSchedule = await _publishScheduleRepository.GetItemsAsync(x => x.VideoId.ToLower() == entity.VideoId.ToLower());
            if (existingPublishSchedule.Count > 0)
                return;

            await _publishScheduleRepository.AddItemAsync(entity);
        }

        public async Task UpdatePublishSchedule(PublishSchedule entity)
        {
            var existingPublishSchedule = await _publishScheduleRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingPublishSchedule.Count == 0)
                return;

            await _publishScheduleRepository.UpdateItemAsync(entity);
        }

        public async Task DeletePublishSchedule(string publishScheduleId)
        {
            var publishSchedule = (await _publishScheduleRepository.GetItemsAsync(x => x.Id == publishScheduleId)).FirstOrDefault();
            if (publishSchedule == null)
                return;

            await _publishScheduleRepository.DeleteItemAsync(publishSchedule.Id);
        }


        // Category methods
        public Task<IList<Category>> FetchCategories(IList<string>? ids = null) => _categoryRepository.GetItemsAsync(x => ids != null ? ids.Contains(x.Id) : true);


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

        // Compilation methods

        public async Task<Compilation?> GetCompilationById(string id) =>
            await _compilationRepository.GetItemAsync(id);

        public async Task<Compilation> SaveCompilation(Compilation entity)
        {
            var existingCompilation = await _compilationRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingCompilation.Count > 0)
                return entity;

            return await _compilationRepository.AddItemAsync(entity);
        }

        public async Task UpdateCompilation(Compilation entity)
        {
            var existingCompilation = await _compilationRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingCompilation.Count == 0)
                return;

            await _compilationRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteCompilation(string compilationId)
        {
            var compilation = (await _compilationRepository.GetItemsAsync(x => x.Id == compilationId)).FirstOrDefault();
            if (compilation == null)
                return;

            await _compilationRepository.DeleteItemAsync(compilation.Id);
        }

        // QueryLink methods
        public Task<IList<QueryLink>> FetchQueryLinks(IList<string>? ids = null) => _queryLinkRepository.GetItemsAsync(x => ids != null ? ids.Contains(x.Id) : true);
        public async Task<QueryLink?> GetQueryLink(string queryLinkId)
        {
            var queryLink = (await _queryLinkRepository.GetItemsAsync(x => x.Id == queryLinkId)).FirstOrDefault();
            return queryLink;
        }
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


        public Task<IList<User>> FetchUsers() => _userRepository.GetItemsAsync();
        public async Task<User?> GetUser(string id) => (await _userRepository.GetItemsAsync(x => x.Id.ToLower() == id.ToLower())).FirstOrDefault();
        public async Task<User?> GetUserByUsername(string userName) => (await _userRepository.GetItemsAsync(x => x.Username.ToLower() == userName.ToLower())).FirstOrDefault();
        public Task UpdateUser(User entity) => _userRepository.UpdateItemAsync(entity);

        public async Task SaveUser(User entity)
        {
            var existingUser = await _userRepository.GetItemsAsync(x => x.Username.ToLower() == entity.Username.ToLower());
            if (existingUser.Count > 0)
                return;
            await _userRepository.AddItemAsync(entity);
        }

        public async Task DeleteUser(string id)
        {
            var shortLink = (await _userRepository.GetItemsAsync(x => x.Id == id)).FirstOrDefault();
            if (shortLink == null)
                return;
            await _userRepository.DeleteItemAsync(shortLink.Id);
        }

        // CustomForm methods
        public Task<IList<CustomForm>> Fetch() => _customFormRepository.GetItemsAsync();

        public async Task SaveCustomForm(CustomForm entity)
        {
            var existingForm = await _customFormRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingForm.Count > 0)
                return;

            await _customFormRepository.AddItemAsync(entity);
        }

        public async Task UpdateCustomForm(CustomForm entity)
        {
            var existingForm = await _customFormRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingForm.Count == 0)
                return;

            await _customFormRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteCustomForm(string customFormId)
        {
            var customForm = (await _customFormRepository.GetItemsAsync(x => x.Id == customFormId)).FirstOrDefault();
            if (customForm == null)
                return;

            await _customFormRepository.DeleteItemAsync(customForm.Id);
        }

        

        // InsightTopic methods
        public Task<IList<InsightTopic>> GetInsightTopics() => _insightTopicRepository.GetItemsAsync();

        public async Task<InsightTopic?> GetInsightTopicById(string id) =>
            await _insightTopicRepository.GetItemAsync(id);

        public async Task SaveInsightTopic(InsightTopic entity)
        {
            var existingTopic = await _insightTopicRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingTopic.Count > 0)
                return;

            await _insightTopicRepository.AddItemAsync(entity);
        }

        public async Task UpdateInsightTopic(InsightTopic entity)
        {
            var existingTopic = await _insightTopicRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingTopic.Count == 0)
                return;

            await _insightTopicRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteInsightTopic(string topicId)
        {
            var topic = (await _insightTopicRepository.GetItemsAsync(x => x.Id == topicId)).FirstOrDefault();
            if (topic == null)
                return;

            await _insightTopicRepository.DeleteItemAsync(topic.Id);
        }

        // InsightNewsItem methods
        public Task<IList<InsightNewsItem>> GetInsightNewsItems() => _insightNewsItemRepository.GetItemsAsync();

        public async Task<IList<InsightNewsItem>> GetInsightNewsItemsByTopicId(string topicId) =>
            await _insightNewsItemRepository.GetItemsAsync(x => x.TopicId == topicId);

        public async Task<IList<InsightNewsItem>> GetInsightNewsItemsByStatus(InsightNewsStatus status) =>
            await _insightNewsItemRepository.GetItemsAsync(x => x.Status == status);

        public async Task<InsightNewsItem?> GetInsightNewsItemById(string id) =>
            await _insightNewsItemRepository.GetItemAsync(id);

        public async Task SaveInsightNewsItem(InsightNewsItem entity)
        {
            var existingNewsItem = await _insightNewsItemRepository.GetItemsAsync(x => x.SourceUrl.ToLower() == entity.SourceUrl.ToLower());
            if (existingNewsItem.Count > 0)
                return;

            await _insightNewsItemRepository.AddItemAsync(entity);
        }

        public async Task UpdateInsightNewsItem(InsightNewsItem entity)
        {
            var existingNewsItem = await _insightNewsItemRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingNewsItem.Count == 0)
                return;

            await _insightNewsItemRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteInsightNewsItem(string newsItemId)
        {
            var newsItem = (await _insightNewsItemRepository.GetItemsAsync(x => x.Id == newsItemId)).FirstOrDefault();
            if (newsItem == null)
                return;

            await _insightNewsItemRepository.DeleteItemAsync(newsItem.Id);
        }

        public async Task UpdateInsightNewsItemStarRating(string newsItemId, int starRating)
        {
            var newsItem = await GetInsightNewsItemById(newsItemId);
            if (newsItem == null)
                return;

            var updatedNewsItem = newsItem.UpdateStarRating(starRating);
            await _insightNewsItemRepository.UpdateItemAsync(updatedNewsItem);
        }

        // InsightContentPlan methods
        public Task<IList<InsightContentPlan>> GetInsightContentPlans() => _insightContentPlanRepository.GetItemsAsync();

        public async Task<IList<InsightContentPlan>> GetInsightContentPlansByTopicId(string topicId) =>
            await _insightContentPlanRepository.GetItemsAsync(x => x.TopicId == topicId);

        public async Task<InsightContentPlan?> GetInsightContentPlanById(string id) =>
            await _insightContentPlanRepository.GetItemAsync(id);

        public async Task SaveInsightContentPlan(InsightContentPlan entity)
        {
            var existingPlan = await _insightContentPlanRepository.GetItemsAsync(x => x.Title.ToLower() == entity.Title.ToLower());
            if (existingPlan.Count > 0)
                return;

            await _insightContentPlanRepository.AddItemAsync(entity);
        }

        public async Task UpdateInsightContentPlan(InsightContentPlan entity)
        {
            var existingPlan = await _insightContentPlanRepository.GetItemsAsync(x => x.Id == entity.Id);
            if (existingPlan.Count == 0)
                return;

            await _insightContentPlanRepository.UpdateItemAsync(entity);
        }

        public async Task DeleteInsightContentPlan(string contentPlanId)
        {
            var contentPlan = (await _insightContentPlanRepository.GetItemsAsync(x => x.Id == contentPlanId)).FirstOrDefault();
            if (contentPlan == null)
                return;

            await _insightContentPlanRepository.DeleteItemAsync(contentPlan.Id);
        }

    }
}

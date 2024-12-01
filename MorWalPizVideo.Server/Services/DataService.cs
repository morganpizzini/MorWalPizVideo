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
        public DataService(IWebHostEnvironment environment, IMatchRepository matchRepository, IProductRepository productRepository, ISponsorRepository sponsorRepository, IPageRepository pageRepository)
        {
            _environment = environment;
            _matchRepository = matchRepository;
            _productRepository = productRepository;
            _sponsorRepository = sponsorRepository;
            _pageRepository = pageRepository;
        }

        public Task<IList<Match>> GetItems() => _matchRepository.GetItemsAsync();
       
        public Task<IList<Product>> GetProducts() => _productRepository.GetItemsAsync();

        public Task<IList<Sponsor>> GetSponsors() => _sponsorRepository.GetItemsAsync();
        public Task<IList<Page>> GetPages() => _pageRepository.GetItemsAsync();
        
    }
}

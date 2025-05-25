using MorWalPizVideo.Server.Models;
using System.Linq.Expressions;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T> GetItemAsync(string id);
        Task<IList<T>> GetItemsAsync();
        Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate);
        Task AddItemAsync(T item);
        Task UpdateItemAsync(T item);
        Task DeleteItemAsync(string id);
    }
    public interface IMatchRepository : IRepository<Match> { }
    public interface IProductRepository : IRepository<Product> { }
    public interface IYTChannelRepository : IRepository<YTChannel> { }
    public interface ISponsorRepository : IRepository<Sponsor> { }
    public interface ISponsorApplyRepository : IRepository<SponsorApply> { }
    public interface IPageRepository : IRepository<Page> { }
    public interface IQueryLinkRepository : IRepository<QueryLink> { }
    public interface IPublishScheduleRepository : IRepository<PublishSchedule> { }
    public interface ICalendarEventRepository : IRepository<CalendarEvent> { }
    public interface IBioLinkRepository : IRepository<BioLink> { }
    public interface IShortLinkRepository : IRepository<ShortLink> { }
    public interface IConfigurationRepository : IRepository<MorWalPizConfiguration> { }
    public interface ICategoryRepository : IRepository<Category>
    {
    }
}

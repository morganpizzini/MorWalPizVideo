﻿using MorWalPizVideo.Server.Models;
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
    public interface ISponsorRepository : IRepository<Sponsor> { }
    public interface IPageRepository : IRepository<Page> { }
    public interface ICalendarEventRepository : IRepository<CalendarEvent> { }
    public interface IShortLinkRepository : IRepository<ShortLink> { }
}

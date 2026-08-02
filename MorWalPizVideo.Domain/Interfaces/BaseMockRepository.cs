using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using System.Linq.Expressions;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public abstract class BaseMockRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly string _fileName;
        private readonly IMockScenario scenario;
        protected BaseMockRepository(IMockScenario scenario, string fileName)
        {
            this.scenario = scenario;
            _fileName = fileName;
        }

        public Task<T> AddItemAsync(T item)
        {
            if (item == null)
                return Task.FromResult(item!);

            return Task.FromResult(scenario.Add(_fileName, item));
        }

        public Task DeleteItemAsync(string id)
        {
            scenario.Delete<T>(_fileName, id);
            return Task.CompletedTask;
        }

        public Task<T> GetItemAsync(string id) =>
            Task.FromResult(
                scenario.Read<T>(_fileName)
                    .OrderByDescending(x => x.CreationDateTime)
                    .FirstOrDefault(x => x.Id == id)!);

        public Task<IList<T>> GetItemsAsync() =>
                Task.FromResult(scenario.Read<T>(_fileName));

        public Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IList<T> result = scenario.Read<T>(_fileName)
                    .Where(predicate.Compile()).ToList();
            return Task.FromResult(result);
        }

        public Task UpdateItemAsync(T item)
        {
            if (item != null)
                scenario.Replace(_fileName, item);
            return Task.CompletedTask;
        }
    }
}

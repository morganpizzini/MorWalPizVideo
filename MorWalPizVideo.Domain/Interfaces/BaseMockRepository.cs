using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MorWalPizVideo.Server.Models;
using System.Linq.Expressions;
using System.Text.Json;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public abstract class BaseMockRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly string _fileName;
        private readonly IHostEnvironment _environment;
        public BaseMockRepository(IHostEnvironment environment, string fileName)
        {
            _environment = environment;
            _fileName = fileName;
        }

        public async Task AddItemAsync(T item)
        {
            if (item == null)
                return;

            var items = ReadJson<T>(_fileName).ToList();
            
            // If item doesn't have an ID, generate one
            if (string.IsNullOrEmpty(item.Id))
            {
                // Generate a new ObjectId-like string for the item
                item = item with { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString() };
            }
            
            // If CreationDateTime is default, set it to now
            if (item.CreationDateTime == default)
            {
                item = item with { CreationDateTime = DateTime.Now };
            }
            
            items.Add(item);
            await WriteJson(items, _fileName);
        }

        public async Task DeleteItemAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            var items = ReadJson<T>(_fileName).ToList();
            var itemToRemove = items.FirstOrDefault(x => x.Id == id);
            
            if (itemToRemove != null)
            {
                items.Remove(itemToRemove);
                await WriteJson(items, _fileName);
            }
        }

        public Task<T> GetItemAsync(string id) =>
            Task.FromResult(
                ReadJson<T>(_fileName)
                    .OrderByDescending(x => x.CreationDateTime)
                    .FirstOrDefault(x => x.Id == id));

        public Task<IList<T>> GetItemsAsync() =>
                Task.FromResult(ReadJson<T>(_fileName));

        public Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IList<T> result = ReadJson<T>(_fileName)
                    .Where(predicate.Compile()).ToList();
            return Task.FromResult(result);
        }

        public async Task UpdateItemAsync(T item)
        {
            if (item?.Id == null)
                return;

            var items = ReadJson<T>(_fileName).ToList();
            var index = items.FindIndex(x => x.Id == item.Id);
            
            if (index >= 0)
            {
                items[index] = item;
                await WriteJson(items, _fileName);
            }
        }
        
        private async Task WriteJson<K>(IList<K> items, string jsonFileName) where K : T
        {
            var directoryPath = Path.Combine(_environment.ContentRootPath, "Data");
            var filePath = Path.Combine(directoryPath, $"{jsonFileName}.json");

            // Ensure directory exists
            Directory.CreateDirectory(directoryPath);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var jsonString = JsonSerializer.Serialize(items.OrderByDescending(x => x.CreationDateTime), options);
            await File.WriteAllTextAsync(filePath, jsonString);
        }

        private IList<K> ReadJson<K>(string jsonFileName) where K : T
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Data", $"{jsonFileName}.json");

            if (!File.Exists(filePath))
            {
                return new List<K>();
            }

            var jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<IList<K>>(jsonString, options)?.OrderByDescending(x => x.CreationDateTime).ToList() ?? new List<K>();
        }
    }
}

using MorWalPizVideo.Server.Models;
using System.Text.Json;

namespace MorWalPizVideo.Server.Services
{
    public class DataService
    {
        private readonly IWebHostEnvironment _environment;
        public DataService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public List<Match> GetItems() => ReadJson<Match>("data").OrderByDescending(x => x.CreationDateTime).ToList();
       
        public List<Product> GetProducts() => ReadJson<Product>("products");

        public List<Sponsor> GetSponsors() => ReadJson<Sponsor>("sponsors");
        

        private List<T> ReadJson<T>(string jsonFileName)
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Data", $"{jsonFileName}.json");
            var jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<List<T>>(jsonString, options)?.ToList() ?? new List<T>();
        }
    }
}

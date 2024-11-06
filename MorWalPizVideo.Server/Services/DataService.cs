using MorWalPizVideo.Server.Models;
using System.Text.Json;

namespace MorWalPizVideo.Server.Services
{
    public class DataService
    {
        private readonly IWebHostEnvironment _environment;

        private List<Match>? Items;
        private List<Product>? Products;
        public DataService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void ResetItems()
        {
            Items = null;
            Products = null;
        }

        public List<Match> GetItems()
        {
            if(Items != null)
            {
                return Items;
            }
            var filePath = Path.Combine(_environment.ContentRootPath, "Data", "data.json");
            var jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            Items = JsonSerializer.Deserialize<List<Match>>(jsonString,options)?
                            .OrderByDescending(x=>x.CreationDateTime).ToList() ?? new List<Match>();
            return Items;
        }

        public List<Product> GetProducts()
        {
            if (Products != null)
            {
                return Products;
            }
            var filePath = Path.Combine(_environment.ContentRootPath, "Data", "products.json");
            var jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            Products = JsonSerializer.Deserialize<List<Product>>(jsonString, options)?.ToList() ?? new List<Product>();
            return Products;
        }
    }
}

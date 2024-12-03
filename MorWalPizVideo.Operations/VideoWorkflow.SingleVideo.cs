using MongoDB.Driver;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class VideoWorkflow
    {
        public static async Task SingleVideo(IMongoCollection<Match> matchCollection, HttpClient client) {
            Console.WriteLine("Enter video ID");
            var element = Console.ReadLine();
            if (string.IsNullOrEmpty(element))
            {
                Console.WriteLine("Not a valid ID");
                return;
            }
            Console.WriteLine("Enter Category");
            var category = Console.ReadLine();
            if (string.IsNullOrEmpty(category))
            {
                Console.WriteLine("Not a valid category");
                return;
            }
            matchCollection.InsertOne(new Match(element, true, category));
            var json = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/reset?k=match");
            json = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/matches");
        }
    }
}

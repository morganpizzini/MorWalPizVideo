using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class VideoWorkflow
    {
        public static async Task SingleVideo(IMongoCollection<Match> matchCollection, HttpClient client) {
            Console.WriteLine("Enter video ID");
            var videoId = Console.ReadLine();
            if (string.IsNullOrEmpty(videoId))
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
            
            matchCollection.InsertOne(new Match(videoId, true, category));

            var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.Match}");
            json = await client.GetStringAsync($"cache/purge/{ApiTagCacheKeys.Matches}");
            json = await client.GetStringAsync("matches");
        }
    }
}

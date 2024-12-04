using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class VideoWorkflow
    {
        public static async Task SubVideo(IMongoCollection<Match> matchCollection, HttpClient client) {
            Console.WriteLine("Enter video ID");
            var element3 = Console.ReadLine();
            if (string.IsNullOrEmpty(element3))
            {
                Console.WriteLine("Not a valid ID");
                return;
            }
            var existingMatch = matchCollection.Find(x => x.ThumbnailUrl == element3).FirstOrDefault();
            if (existingMatch == null)
            {
                Console.WriteLine("Match do not exists");
                return;
            }

            Console.WriteLine("Enter video ID");
            var videoId = Console.ReadLine();
            if (string.IsNullOrEmpty(videoId))
            {
                Console.WriteLine("Not a valid ID");
                return;
            }
            Console.WriteLine("Enter Category");
            var category3 = Console.ReadLine();
            if (string.IsNullOrEmpty(category3))
            {
                Console.WriteLine("Not a valid category");
                return;
            }
            existingMatch = existingMatch with { Videos = [.. existingMatch.Videos, new Video(videoId, category3)] };

            await matchCollection.ReplaceOneAsync(Builders<Match>.Filter.Eq(e => e.Id, existingMatch.Id), existingMatch);

            var json3 = await client.GetStringAsync($"https://morwalpiz.azurewebsites.net/api/reset?k={CacheKeys.Match}");
            json3 = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/matches");
        }
    }
}

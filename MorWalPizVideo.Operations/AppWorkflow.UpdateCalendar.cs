using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class AppWorkflow
    {
        public static async Task UpdateCalendarEvent(IMongoCollection<CalendarEvent> collection, HttpClient client) {
            var results = Utils.AskFor("Title", "MatchId");
            if (results.Any(string.IsNullOrEmpty))
            {
                Console.WriteLine("Provided values are not valid");
            }

            var existing = collection.Find(x => x.Title== results[0]).FirstOrDefault();

            if (existing == null)
            {
                Console.WriteLine("Calendar event do not exists");
                return;
            }

            existing = existing with { MatchId = results[1] };
            
            await collection.ReplaceOneAsync(Builders<CalendarEvent>.Filter.Eq(e => e.Id, existing.Id), existing);
            
            var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.CalendarEvents}");
            json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.CalendarEvents}");
        }
    }
}

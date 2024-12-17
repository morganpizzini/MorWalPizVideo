using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System.Globalization;

namespace MorWalPizVideo.Operations
{
    static partial class AppWorkflow
    {
        public static async Task AddCalendarEvent(IMongoCollection<CalendarEvent> collection, HttpClient client) {
            var results = Utils.AskFor("Title", "Date", "Category", "Description", "MatchId");
            if (results.Take(3).Any(string.IsNullOrEmpty))
            {
                Console.WriteLine("Provided values are not valid");
            }

            collection.InsertOne(new CalendarEvent(results[0], results[3], DateOnly.ParseExact(results[1], "yy-MM-dd", CultureInfo.InvariantCulture), results[2], results[4]));

            var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.CalendarEvents}");
            json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.CalendarEvents}");

        }
    }
}

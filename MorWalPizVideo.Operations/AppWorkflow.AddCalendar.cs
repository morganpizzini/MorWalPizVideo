using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System.Globalization;

namespace MorWalPizVideo.Operations
{
    static partial class AppWorkflow
    {
        public static string[] AskFor(params string[] args) { 
            var results = new string[args.Length];
            for (int i = 0; i < args.Length; i++) { 
                Console.WriteLine($"Enter {args[i]}");
                results[i] = Console.ReadLine() ?? string.Empty;
            }
            return results;
        }
        public static async Task AddCalendarEvent(IMongoCollection<CalendarEvent> collection, HttpClient client) {
            var results = AskFor("Title", "Date", "Category", "Description", "MatchId");
            if (results.Take(3).Any(string.IsNullOrEmpty))
            {
                Console.WriteLine("Provided values are not valid");
            }

            collection.InsertOne(new CalendarEvent(results[0], results[3], DateOnly.ParseExact(results[1], "yy-MM-dd", CultureInfo.InvariantCulture), results[2], results[4]));

            var json = await client.GetStringAsync($"https://morwalpiz.azurewebsites.net/api/reset?k={CacheKeys.CalendarEvents}");
        }
    }
}

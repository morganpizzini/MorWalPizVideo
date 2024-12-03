using MongoDB.Driver;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class AppWorkflow
    {
        public static string[] AskFor(params string[] args) { 
            var results = new string[args.Length];
            for (int i = 0; i < args.Length; i++) { 
                Console.WriteLine($"Enter {args}");
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

            var x = DateOnly.Parse(results[1]);

            return;

            collection.InsertOne(new CalendarEvent(results[0], results[3], DateOnly.Parse(results[1]), results[2], results[4]));
            var json = await client.GetStringAsync("https://morwalpiz.azurewebsites.net/api/reset?k=calendar");
        }
    }
}

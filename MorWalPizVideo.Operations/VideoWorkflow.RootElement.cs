using MongoDB.Driver;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class VideoWorkflow
    {
        public static void RootElement(IMongoCollection<Match> matchCollection) {
            Console.WriteLine("Enter root ID");
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
            Console.WriteLine("Enter title");
            var title = Console.ReadLine();
            if (string.IsNullOrEmpty(title))
            {
                Console.WriteLine("Not a valid title");
                return;
            }
            Console.WriteLine("Enter description");
            var description = Console.ReadLine();
            if (string.IsNullOrEmpty(description))
            {
                Console.WriteLine("Not a valid description");
                return;
            }
            Console.WriteLine("Enter url");
            var url = Console.ReadLine();
            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine("Not a valid url");
                return;
            }
            matchCollection.InsertOne(new Match(videoId, title, description, url, [], category));
        }
    }
}

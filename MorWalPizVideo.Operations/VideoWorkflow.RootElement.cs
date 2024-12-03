using MongoDB.Driver;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class VideoWorkflow
    {
        public static void RootElement(IMongoCollection<Match> matchCollection) {
            Console.WriteLine("Enter root ID");
            var element2 = Console.ReadLine();
            if (string.IsNullOrEmpty(element2))
            {
                Console.WriteLine("Not a valid ID");
                return;
            }
            Console.WriteLine("Enter Category");
            var category2 = Console.ReadLine();
            if (string.IsNullOrEmpty(category2))
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
            var url = Console.ReadLine();
            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine("Not a valid url");
                return;
            }
            matchCollection.InsertOne(new Match(element2, title, description, url, [], category2));
        }
    }
}

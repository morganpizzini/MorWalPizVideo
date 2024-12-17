using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class BioWorkflow
    {
        public static async Task DeleteBioLink(IMongoCollection<BioLink> collection, HttpClient client)
        {
            var results = Utils.AskFor("Title");
            if (results.Take(3).Any(string.IsNullOrEmpty))
            {
                Console.WriteLine("Provided values are not valid");
                return;
            }
            var entity = collection.Find(x => x.Title.ToLower() == results[0]).FirstOrDefault();
            if (entity == null)
            {
                Console.WriteLine("Bio link has not found");
                return;
            }
            collection.DeleteOne(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id));

            var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.BioLink}");
            json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.BioLinks}");
            return;
        }
    }
}

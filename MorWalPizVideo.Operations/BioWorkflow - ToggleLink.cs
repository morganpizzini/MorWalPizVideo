using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class BioWorkflow
    {
        public static async Task ToggleBioLink(IMongoCollection<BioLink> collection, HttpClient client)
        {
            var results = Utils.AskFor("Title");
            if (results.Any(string.IsNullOrEmpty))
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

            entity = entity with { Enable= !entity.Enable };

            var updates = new List<WriteModel<BioLink>>();

            updates.Add(new ReplaceOneModel<BioLink>(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id),entity));

            var result = await collection.BulkWriteAsync(updates);

            var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.BioLink}");
            json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.BioLinks}");
            return;
        }
    }
}

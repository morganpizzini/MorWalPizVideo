using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class BioWorkflow
    {
        public static async Task CreateBioLink(IMongoCollection<BioLink> collection, HttpClient client)
        {
            var results = Utils.AskFor("Title", "Description", "Url", "Icon", "Order");
            if (results.Any(string.IsNullOrEmpty))
            {
                Console.WriteLine("Provided values are not valid");
                return;
            }
            if (!int.TryParse(results[4],out var order))
            {
                Console.WriteLine("Order is not a valid number");
                return;
            }

            var entity = new BioLink(results[0], results[1], results[2], results[3], order);

            var items = collection.Find(x => x.Order >= entity.Order)
                .ToList();

            var updates = new List<WriteModel<BioLink>>();
            foreach (var item in items)
            {
                var filter = Builders<BioLink>.Filter.Eq(x => x.Id, item.Id);
                var update = Builders<BioLink>.Update.Set(x => x.Order, item.Order+1);
                updates.Add(new UpdateOneModel<BioLink>(filter, update));
            }

            updates.Add(new InsertOneModel<BioLink>(entity));

            var result = await collection.BulkWriteAsync(updates);

            var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.BioLink}");
            json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.BioLinks}");
            return;
        }
    }
}

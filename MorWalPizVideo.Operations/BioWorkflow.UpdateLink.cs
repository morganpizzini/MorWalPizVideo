using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Operations
{
    static partial class BioWorkflow
    {
        public static async Task UpdateBioLink(IMongoCollection<BioLink> collection, HttpClient client)
        {
            var results = Utils.AskFor("Title", "NewTitle", "Description", "Order");
            if (results.Take(3).Any(string.IsNullOrEmpty))
            {
                Console.WriteLine("Provided values are not valid");
                return;
            }
            var updateOrder = !string.IsNullOrEmpty(results[3]);

            if (!int.TryParse(results[4], out var order))
            {
                Console.WriteLine("Order is not a valid number");
                return;
            }

            var entity = collection.Find(x => x.Title.ToLower() == results[0]).FirstOrDefault();
            if (entity == null)
            {
                Console.WriteLine("Bio link has not found");
                return;
            }

            var orderChange = updateOrder && entity.Order != order;

            entity = entity with { Title = results[1], Description = results[2], Order = order };
            var updates = new List<WriteModel<BioLink>>();
            if (orderChange)
            {
                var items = collection.Find(x => x.Order >= entity.Order)
                    .ToList();

                foreach (var item in items)
                {
                    var filter = Builders<BioLink>.Filter.Eq(x => x.Id, item.Id);
                    var update = Builders<BioLink>.Update.Set(x => x.Order, item.Order+1);
                    updates.Add(new UpdateOneModel<BioLink>(filter, update));
                }
            }

            updates.Add(new ReplaceOneModel<BioLink>(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id),entity));

            var result = await collection.BulkWriteAsync(updates);

            var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.BioLink}");
            json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.BioLinks}");
            return;
        }
    }
}

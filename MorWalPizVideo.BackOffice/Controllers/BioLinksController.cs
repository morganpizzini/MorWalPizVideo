using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Controllers;
public class ToggleBioLinkRequest
{
    public string Title { get; set; } = string.Empty;
}

public class CreateBioLinkRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateBioLinkRequest
{
    public string Title { get; set; } = string.Empty;
    public string NewTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class BioLinksController : ApplicationController
{
    private readonly IMongoDatabase database;
    private readonly IHttpClientFactory client;
    public BioLinksController(IMongoDatabase _database, IHttpClientFactory _clientFactory)
    {
        database = _database;
        client = _clientFactory;
    }
    [HttpPost]
    public async Task<IActionResult> CreateBioLink(CreateBioLinkRequest request)
    {
        var collection = database.GetCollection<BioLink>(DbCollections.BioLinks);

        var entity = new BioLink(request.Title, request.Description, request.Url, request.Icon, request.Order);

        var items = collection.Find(x => x.Order >= entity.Order)
            .ToList();

        var updates = new List<WriteModel<BioLink>>();
        foreach (var item in items)
        {
            var filter = Builders<BioLink>.Filter.Eq(x => x.Id, item.Id);
            var update = Builders<BioLink>.Update.Set(x => x.Order, item.Order + 1);
            updates.Add(new UpdateOneModel<BioLink>(filter, update));
        }

        updates.Add(new InsertOneModel<BioLink>(entity));

        await collection.BulkWriteAsync(updates);

        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);
        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.BioLinks}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.BioLinks}");
        return NoContent();
    }
    [HttpPut]
    public async Task<IActionResult> UpdateBioLink(UpdateBioLinkRequest request)
    {
        var collection = database.GetCollection<BioLink>(DbCollections.BioLinks);

        var entity = collection.Find(x => x.Title.ToLower() == request.Title).FirstOrDefault();
        if (entity == null)
            return BadRequest("Bio link has not found");

        var orderChange = entity.Order != request.Order;

        entity = entity with { Title = request.NewTitle, Description = request.Description, Order = request.Order };
        var updates = new List<WriteModel<BioLink>>();
        if (orderChange)
        {
            var items = collection.Find(x => x.Order >= entity.Order)
                .ToList();

            foreach (var item in items)
            {
                var filter = Builders<BioLink>.Filter.Eq(x => x.Id, item.Id);
                var update = Builders<BioLink>.Update.Set(x => x.Order, item.Order + 1);
                updates.Add(new UpdateOneModel<BioLink>(filter, update));
            }
        }

        updates.Add(new ReplaceOneModel<BioLink>(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id), entity));

        var result = await collection.BulkWriteAsync(updates);


        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);
        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.BioLinks}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.BioLinks}");
        return NoContent();
    }

    [HttpPut("toggle")]
    public async Task<IActionResult> ToggleBioLink(ToggleBioLinkRequest request)
    {
        var collection = database.GetCollection<BioLink>(DbCollections.BioLinks);

        var entity = collection.Find(x => x.Title.ToLower() == request.Title).FirstOrDefault();
        if (entity == null)
        {
            return BadRequest("Bio link has not found");
        }

        entity = entity with { Enable = !entity.Enable };

        var updates = new List<WriteModel<BioLink>>();

        updates.Add(new ReplaceOneModel<BioLink>(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id), entity));

        var result = await collection.BulkWriteAsync(updates);

        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);
        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.BioLinks}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.BioLinks}");
        return NoContent();
    }

    [HttpDelete("{title}")]
    public async Task<IActionResult> DeleteBioLink(string title)
    {
        var collection = database.GetCollection<BioLink>(DbCollections.BioLinks);

        var entity = collection.Find(x => x.Title.ToLower() == title).FirstOrDefault();
        if (entity == null)
        {
            return BadRequest("Bio link has not found");
        }
        collection.DeleteOne(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id));

        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);
        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.BioLinks}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.BioLinks}");
        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MorWalPizVideo.BackOffice.Services;
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

public class BioLinksController : ApplicationControllerBase
{
    private readonly IMongoDatabase database;
    private readonly ICrossApiService client;
    public BioLinksController(IMongoDatabase _database, ICrossApiService _clientFactory)
    {
        database = _database;
        client = _clientFactory;
    }
    [HttpPost]
    public async Task<IActionResult> CreateBioLink(CreateBioLinkRequest request)
    {
        var collection = database.GetCollection<BioLink>(DbCollections.BioLinks);

        var entity = new BioLink(request.Title, request.Description, request.Url, request.Icon, request.Order);

        await collection.UpdateManyAsync(
            Builders<BioLink>.Filter.Gte(x => x.Order, entity.Order),
            Builders<BioLink>.Update.Inc(x => x.Order, 1));

        await collection.InsertOneAsync(entity);

        await client.ResetCache(CacheKeys.BioLinks);
        await client.PurgeCache(ApiTagCacheKeys.BioLinks);
        return NoContent();
    }
    [HttpPut]
    public async Task<IActionResult> UpdateBioLink(UpdateBioLinkRequest request)
    {
        var collection = database.GetCollection<BioLink>(DbCollections.BioLinks);

        var entity = await collection.Find(x => x.Title.ToLower() == request.Title).FirstOrDefaultAsync();
        if (entity == null)
            return BadRequest("Bio link has not found");

        var oldOrder = entity.Order;
        var newOrder = request.Order;

        entity = entity with { Title = request.NewTitle, Description = request.Description, Order = newOrder };

        if (newOrder < oldOrder)
        {
            await collection.UpdateManyAsync(
                Builders<BioLink>.Filter.And(
                    Builders<BioLink>.Filter.Gte(x => x.Order, newOrder),
                    Builders<BioLink>.Filter.Lt(x => x.Order, oldOrder)),
                Builders<BioLink>.Update.Inc(x => x.Order, 1));
        }
        else if (newOrder > oldOrder)
        {
            await collection.UpdateManyAsync(
                Builders<BioLink>.Filter.And(
                    Builders<BioLink>.Filter.Gt(x => x.Order, oldOrder),
                    Builders<BioLink>.Filter.Lte(x => x.Order, newOrder)),
                Builders<BioLink>.Update.Inc(x => x.Order, -1));
        }

        await collection.ReplaceOneAsync(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id), entity);

        await client.ResetCache(CacheKeys.BioLinks);
        await client.PurgeCache(ApiTagCacheKeys.BioLinks);
        return NoContent();
    }

    [HttpPut("toggle")]
    public async Task<IActionResult> ToggleBioLink(ToggleBioLinkRequest request)
    {
        var collection = database.GetCollection<BioLink>(DbCollections.BioLinks);

        var entity = await collection.Find(x => x.Title.ToLower() == request.Title).FirstOrDefaultAsync();
        if (entity == null)
        {
            return BadRequest("Bio link has not found");
        }

        entity = entity with { Enable = !entity.Enable };

        await collection.ReplaceOneAsync(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id), entity);

        await client.ResetCache(CacheKeys.BioLinks);
        await client.PurgeCache(ApiTagCacheKeys.BioLinks);
        return NoContent();
    }

    [HttpDelete("{title}")]
    public async Task<IActionResult> DeleteBioLink(string title)
    {
        var collection = database.GetCollection<BioLink>(DbCollections.BioLinks);

        var entity = await collection.Find(x => x.Title.ToLower() == title).FirstOrDefaultAsync();
        if (entity == null)
        {
            return BadRequest("Bio link has not found");
        }
        await collection.DeleteOneAsync(Builders<BioLink>.Filter.Eq(e => e.Id, entity.Id));

        await client.ResetCache(CacheKeys.BioLinks);
        await client.PurgeCache(ApiTagCacheKeys.BioLinks);
        return NoContent();
    }
}

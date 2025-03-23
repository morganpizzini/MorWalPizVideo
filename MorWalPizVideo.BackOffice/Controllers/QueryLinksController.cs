using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Controllers;
public class CreateQueryLinkRequest
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class UpdateQueryLinkRequest
{
    public string Title { get; set; } = string.Empty;
    public string NewTitle { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class QueryLinksController : ApplicationController
{
    private readonly IMongoDatabase database;
    public QueryLinksController(IMongoDatabase _database)
    {
        database = _database;
    }

    [HttpGet]
    public async Task<IActionResult> GetShortLink()
    {
        var collection = database.GetCollection<QueryLink>(DbCollections.QueryLinks);

        var entities = (await collection.FindAsync(x => true)).ToList();

        return Ok(entities);
    }

    [HttpPost]
    public IActionResult CreateQueryLink(CreateQueryLinkRequest request)
    {
        var collection = database.GetCollection<QueryLink>(DbCollections.QueryLinks);

        collection.InsertOne(new QueryLink(request.Title, request.Value));

        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateQueryLink(UpdateQueryLinkRequest request)
    {
        var collection = database.GetCollection<QueryLink>(DbCollections.QueryLinks);

        var entity = collection.Find(x => x.Title.ToLower() == request.Title).FirstOrDefault();
        if (entity == null)
            return BadRequest("Query link has not found");

        entity = entity with { Title = request.NewTitle, Value = request.Value };

        await collection.ReplaceOneAsync(Builders<QueryLink>.Filter.Eq(e => e.Id, entity.Id), entity);

        return NoContent();
    }

    [HttpDelete("{title}")]
    public IActionResult DeleteQueryLink(string title)
    {
        var collection = database.GetCollection<QueryLink>(DbCollections.QueryLinks);

        var entity = collection.Find(x => x.Title.ToLower() == title).FirstOrDefault();
        if (entity == null)
        {
            return BadRequest("Query link has not found");
        }
        collection.DeleteOne(Builders<QueryLink>.Filter.Eq(e => e.Id, entity.Id));

        return NoContent();
    }
}

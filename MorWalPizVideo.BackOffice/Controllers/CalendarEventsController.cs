using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System.Globalization;

namespace MorWalPizVideo.BackOffice.Controllers;
public class AddCalendarRequest
{
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;
}
public class UpdateCalendarRequest
{
    public string MatchId { get; set; } = string.Empty;
}
public class CalendarEventsController : ApplicationController
{
    private readonly IMongoDatabase database;
    private readonly IHttpClientFactory client;
    public CalendarEventsController(IMongoDatabase _database, IHttpClientFactory _clientFactory)
    {
        database = _database;
        client = _clientFactory;
    }
    [HttpPost]
    public async Task<IActionResult> AddCalendar(AddCalendarRequest request)
    {
        var collection = database.GetCollection<CalendarEvent>(DbCollections.CalendarEvents);

        collection.InsertOne(new CalendarEvent(request.Title, request.Description, DateOnly.ParseExact(request.Date, "yy-MM-dd", CultureInfo.InvariantCulture), request.Category, request.Category));

        using var client = this.client.CreateClient("MorWalPiz");
        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.CalendarEvents}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.CalendarEvents}");

        return NoContent();
    }
    [HttpPut("{title}")]
    public async Task<IActionResult> AddCalendar(string title,[FromBody]UpdateCalendarRequest request)
    {
        var collection = database.GetCollection<CalendarEvent>(DbCollections.CalendarEvents);
        
        var existing = collection.Find(x => x.Title == title).FirstOrDefault();

        if (existing == null)
        {
            return BadRequest("Calendar event do not exists");
        }

        existing = existing with { MatchId = request.MatchId };

        await collection.ReplaceOneAsync(Builders<CalendarEvent>.Filter.Eq(e => e.Id, existing.Id), existing);

        using var client = this.client.CreateClient("MorWalPiz");
        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.CalendarEvents}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.CalendarEvents}");

        return NoContent();
    }
}

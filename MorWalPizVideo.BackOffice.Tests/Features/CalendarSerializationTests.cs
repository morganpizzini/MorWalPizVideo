using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public class CalendarSerializationTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    internal static CalendarEvent Sample() => new("Calendar title", "Description",
        new DateTime(2020, 1, 2, 10, 30, 0, DateTimeKind.Utc),
        new DateTime(2020, 1, 3, 11, 30, 0, DateTimeKind.Utc), [], "match-id")
    {
        Id = "507f1f77bcf86cd799439011",
        ChannelId = "channel-a",
        CreationDateTime = new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
        MatchUrl = "video-or-playlist"
    };

    [Fact]
    public void LegacyPublicEntity_HasEnrichedWireShape()
    {
        var json = JsonSerializer.SerializeToElement(Sample(), WebJson);
        Assert.Equal(new[] { "categories", "channelId", "creationDateTime", "description", "endDate", "id", "matchId", "matchUrl", "oldEvent", "startDate", "title" },
            json.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.True(json.GetProperty("oldEvent").GetBoolean());
        Assert.Equal("video-or-playlist", json.GetProperty("matchUrl").GetString());
        Assert.Equal("2020-01-02T10:30:00Z", json.GetProperty("startDate").GetString());
        Assert.Equal("channel-a", json.GetProperty("channelId").GetString());
        Assert.Equal("507f1f77bcf86cd799439011", json.GetProperty("id").GetString());
    }

    [Fact]
    public void ExplicitPublicDto_PreservesEveryLegacyFieldAndValue()
    {
        var entity = Sample() with { Categories = [new CategoryRef("category-id", "Category title")] };
        var legacy = JsonSerializer.SerializeToElement(entity, WebJson);
        var current = JsonSerializer.SerializeToElement(ContractUtils.ConvertPublic(entity), WebJson);
        Assert.Equal(legacy.EnumerateObject().Select(property => property.Name).Order(), current.EnumerateObject().Select(property => property.Name).Order());
        foreach (var property in legacy.EnumerateObject())
            Assert.Equal(property.Value.GetRawText(), current.GetProperty(property.Name).GetRawText());
    }

    [Fact]
    public void LegacyAdminContract_HasDifferentWireShape()
    {
        var json = JsonSerializer.SerializeToElement(ContractUtils.Convert(Sample()), WebJson);
        Assert.Equal(new[] { "categories", "channelId", "description", "endDate", "id", "matchId", "matchUrl", "revision", "startDate", "title" },
            json.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.False(json.TryGetProperty("oldEvent", out _));
        Assert.False(json.TryGetProperty("creationDateTime", out _));
    }

    [Fact]
    public void ExplicitRequest_AcceptsLegacyJsonAndUsesServerChannel()
    {
        var legacy = Sample() with { Categories = [new CategoryRef("category-id", "Category title")] };
        var request = JsonSerializer.Deserialize<SaveCalendarEventRequest>(JsonSerializer.Serialize(legacy, WebJson), WebJson)!;
        var current = request.ToEntity("server-channel");
        Assert.Equal(legacy.Id, current.Id);
        Assert.Equal(legacy.Title, current.Title);
        Assert.Equal(legacy.Description, current.Description);
        Assert.Equal(legacy.StartDate, current.StartDate);
        Assert.Equal(legacy.EndDate, current.EndDate);
        Assert.Equal(legacy.CreationDateTime, current.CreationDateTime);
        Assert.Equal(legacy.Categories, current.Categories);
        Assert.Equal(legacy.MatchId, current.MatchId);
        Assert.Equal(legacy.ChannelId, request.ChannelId);
        Assert.Equal("server-channel", current.ChannelId);
        Assert.Null(request.Revision);
    }

    [Fact]
    public void LegacyBson_RoundTripsIdentityWithoutComputedFields()
    {
        var document = Sample().ToBsonDocument();
        document.Remove("revision");
        Assert.Equal(BsonType.ObjectId, document["_id"].BsonType);
        Assert.False(document.Contains("MatchUrl"));
        Assert.False(document.Contains("OldEvent"));
        var restored = BsonSerializer.Deserialize<CalendarEvent>(document);
        Assert.Equal(0, restored.Revision);
        Assert.Equal(Sample().Id, restored.Id);
        Assert.Equal(Sample().ChannelId, restored.ChannelId);
        Assert.Equal(Sample().StartDate, restored.StartDate);
        Assert.Equal(Sample().EndDate, restored.EndDate);
        Assert.Equal(Sample().CreationDateTime, restored.CreationDateTime);
        Assert.Equal(Sample().MatchId, restored.MatchId);
        Assert.Empty(restored.Categories);
        Assert.True(string.IsNullOrEmpty(restored.MatchUrl));
    }
}
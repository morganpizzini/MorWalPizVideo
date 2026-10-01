using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class CalendarMongoTests
{
    [CalendarMongoFact]
    public async Task Independent_clients_have_one_atomic_winner_and_channel_scoped_stale_delete()
    {
        await using var fixture = new Fixture();
        var entity = await new CalendarService(fixture.First).CreateAsync(CalendarSerializationTests.Sample(), "channel-a");
        var results = await Task.WhenAll(
            fixture.First.ReplaceAsync(entity with { Title = "First writer" }, 1),
            fixture.Second.ReplaceAsync(entity with { Title = "Second writer" }, 1));
        Assert.Equal(1, results.Count(success => success));
        Assert.Equal(2, (await fixture.Second.GetItemAsync(entity.Id)).Revision);
        Assert.False(await fixture.First.DeleteAsync("channel-a", entity.Id, 1));
        Assert.False(await fixture.First.DeleteAsync("channel-b", entity.Id, 2));
        Assert.True(await fixture.Second.DeleteAsync("channel-a", entity.Id, 2));
    }

    [CalendarMongoFact]
    public async Task Missing_revision_bson_and_generic_legacy_mutations_participate_in_cas()
    {
        await using var fixture = new Fixture();
        var legacy = CalendarSerializationTests.Sample();
        var bson = legacy.ToBsonDocument();
        bson.Remove("revision");
        await fixture.Database.GetCollection<BsonDocument>(DbCollections.CalendarEvents).InsertOneAsync(bson);
        var service = new CalendarService(fixture.First);
        Assert.Equal(0, (await service.GetAsync(legacy.Id))!.Revision);
        var updated = (await service.UpdateAsync(legacy.Id, legacy, legacy.ChannelId, 0))!;
        Assert.Equal(1, updated.Revision);
        await Assert.ThrowsAsync<CalendarConflictException>(() => new CalendarService(fixture.Second).UpdateAsync(legacy.Id, legacy, legacy.ChannelId, 0));
        IRepository<CalendarEvent> generic = fixture.Second;
        await generic.UpdateItemAsync(legacy with { Description = "Legacy writer" });
        Assert.Equal(2, (await service.GetAsync(legacy.Id))!.Revision);
        await Assert.ThrowsAsync<CalendarConflictException>(() => service.DeleteAsync(legacy.Id, legacy.ChannelId, 1));
        await generic.DeleteItemAsync(legacy.Id);
        Assert.Null(await service.GetAsync(legacy.Id));
    }

    private sealed class CalendarMongoFactAttribute : FactAttribute
    {
        public CalendarMongoFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CALENDAR_TEST_MONGO_URI")))
                Skip = "Requires CALENDAR_TEST_MONGO_URI pointing to an explicitly disposable Mongo test server.";
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly MongoClient firstClient;
        private readonly string name = $"calendar_tests_{Guid.NewGuid():N}";
        public IMongoDatabase Database { get; }
        public CalendarEventRepository First { get; }
        public CalendarEventRepository Second { get; }

        public Fixture()
        {
            var uri = Environment.GetEnvironmentVariable("CALENDAR_TEST_MONGO_URI")!;
            firstClient = new MongoClient(uri);
            Database = firstClient.GetDatabase(name);
            First = new(Database);
            Second = new(new MongoClient(uri).GetDatabase(name));
        }

        public async ValueTask DisposeAsync() => await firstClient.DropDatabaseAsync(name);
    }
}
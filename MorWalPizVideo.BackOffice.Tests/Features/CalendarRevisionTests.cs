using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class CalendarRevisionTests(PageControllerWebApplicationFactory factory) : IClassFixture<PageControllerWebApplicationFactory>
{
    private ICalendarEventRepository Repository => factory.Services.GetRequiredService<ICalendarEventRepository>();
    private CalendarService Service => new(Repository);
    private static CalendarEvent Sample() => CalendarSerializationTests.Sample() with
    { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Title = $"Revision-{Guid.NewGuid():N}" };

    [Fact]
    public async Task Two_writers_and_stale_delete_have_one_winner()
    {
        var created = await Service.CreateAsync(Sample(), "channel-a");
        async Task<bool> Save(string title)
        {
            try { await new CalendarService(Repository).UpdateAsync(created.Id, created with { Title = title }, created.ChannelId, created.Revision); return true; }
            catch (CalendarConflictException) { return false; }
        }
        var results = await Task.WhenAll(Task.Run(() => Save("first")), Task.Run(() => Save("second")));
        Assert.Equal(1, results.Count(success => success));
        var current = (await Service.GetAsync(created.Id))!;
        Assert.Equal(2, current.Revision);
        await Assert.ThrowsAsync<CalendarConflictException>(() => Service.DeleteAsync(created.Id, created.ChannelId, 1));
        Assert.False(await Service.DeleteAsync(created.Id, "other", 2));
        Assert.Null(await Service.UpdateAsync(created.Id, current, "other", 2));
        Assert.True(await Service.DeleteAsync(created.Id, created.ChannelId, 2));
    }

    [Fact]
    public async Task Missing_revision_document_and_legacy_callers_advance_the_baseline()
    {
        var legacy = Sample();
        factory.Services.GetRequiredService<IMockScenario>().Add("calendarEvents", legacy);
        Assert.Equal(0, (await Service.GetAsync(legacy.Id))!.Revision);
        var migrated = (await Service.UpdateAsync(legacy.Id, legacy, legacy.ChannelId, 0))!;
        Assert.Equal(1, migrated.Revision);
        await Assert.ThrowsAsync<CalendarConflictException>(() => Service.UpdateAsync(legacy.Id, legacy, legacy.ChannelId, 0));
        var blind = (await Service.UpdateAsync(legacy.Id, legacy with { Description = "Legacy save" }, legacy.ChannelId))!;
        Assert.Equal(2, blind.Revision);
        IRepository<CalendarEvent> generic = Repository;
        await generic.UpdateItemAsync(legacy with { Description = "Generic legacy save" });
        Assert.Equal(3, (await Service.GetAsync(legacy.Id))!.Revision);
        await Assert.ThrowsAsync<CalendarConflictException>(() => Service.DeleteAsync(legacy.Id, legacy.ChannelId, blind.Revision));
        await generic.DeleteItemAsync(legacy.Id);
        Assert.Null(await Service.GetAsync(legacy.Id));
    }

    [Fact]
    public async Task DataService_compatibility_methods_delegate_and_advance_revision()
    {
        var dataService = factory.Services.GetRequiredService<DataService>();
        var entity = Sample();
        await dataService.SaveCalendarEvent(entity);
        Assert.Equal(1, (await Service.GetAsync(entity.Id))!.Revision);
        await dataService.UpdateCalendarEvent(entity);
        Assert.Equal(2, (await Service.GetAsync(entity.Id))!.Revision);
        await dataService.UpdateCalendarEvent(entity, entity.ChannelId);
        Assert.Equal(3, (await Service.GetAsync(entity.Id))!.Revision);
        await dataService.UpdateCalendarEvent(entity, "other");
        await dataService.DeleteCalendarEvent(entity.Id, "other");
        Assert.Equal(3, (await Service.GetAsync(entity.Id))!.Revision);
        await dataService.DeleteCalendarEvent(entity.Id, entity.ChannelId);
        Assert.Null(await Service.GetAsync(entity.Id));
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(9_007_199_254_740_991L)]
    public async Task Invalid_revisions_are_rejected(long revision)
    {
        var created = await Service.CreateAsync(Sample(), "channel-a");
        await Assert.ThrowsAsync<CalendarValidationException>(() => Service.UpdateAsync(created.Id, created, created.ChannelId, revision));
        await Assert.ThrowsAsync<CalendarValidationException>(() => Service.DeleteAsync(created.Id, created.ChannelId, revision));
    }
}
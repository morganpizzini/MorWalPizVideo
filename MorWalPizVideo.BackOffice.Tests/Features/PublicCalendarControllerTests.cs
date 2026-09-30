using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class PublicCalendarControllerTests(ServerApiWebApplicationFactory factory) : IClassFixture<ServerApiWebApplicationFactory>
{
    [Fact]
    public async Task Anonymous_public_read_keeps_computed_fields_dates_categories_and_match_enrichment_without_revision()
    {
        var matches = await factory.Services.GetRequiredService<IYouTubeContentRepository>().GetItemsAsync();
        var match = matches.First(item => !item.IsPrivate);
        var entity = CalendarSerializationTests.Sample() with
        {
            CreationDateTime = DateTime.UtcNow, MatchId = match.Id,
            Categories = [new CategoryRef("category-id", "Category title")], Revision = 7
        };
        var repository = factory.Services.GetRequiredService<ICalendarEventRepository>();
        await repository.AddItemAsync(entity);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/calendarEvents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = json.RootElement.EnumerateArray().Single(item => item.GetProperty("id").GetString() == entity.Id);
        Assert.True(item.GetProperty("oldEvent").GetBoolean());
        Assert.False(item.TryGetProperty("revision", out _));
        Assert.Equal(match.ContentType == YoutubeContentType.SingleVideo ? match.ContentId : match.Url, item.GetProperty("matchUrl").GetString());
        Assert.Equal(entity.ChannelId, item.GetProperty("channelId").GetString());
        Assert.Equal(entity.StartDate, item.GetProperty("startDate").GetDateTime());
        Assert.Equal(entity.EndDate, item.GetProperty("endDate").GetDateTime());
        Assert.Equal("Category title", item.GetProperty("categories")[0].GetProperty("title").GetString());
    }
}
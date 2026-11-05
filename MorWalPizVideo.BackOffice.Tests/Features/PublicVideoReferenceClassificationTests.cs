using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:ServerAPI")]
public sealed class PublicVideoReferenceClassificationTests
{
    [Theory]
    [InlineData(1, YoutubeContentType.Collection)]
    [InlineData(2, YoutubeContentType.SingleVideo)]
    public async Task All_public_projection_paths_derive_type_without_rewriting_archival_data(
        int count, YoutubeContentType legacyType)
    {
        await using var factory = new ServerApiWebApplicationFactory();
        var repository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = await repository.GetItemAsync(PrimaryScenario.MatchId);
        var match = source with
        {
            ContentType = legacyType,
            VideoRefs = Enumerable.Range(0, count).Select(index =>
                new VideoRef($"attached-{index}", [], channelIds: [PrimaryScenario.ChannelId])).ToArray()
        };
        await repository.UpdateItemAsync(match);
        var channels = factory.Services.GetRequiredService<IYTChannelRepository>();
        var channel = (await channels.GetItemsAsync()).Single(item => item.ChannelId == PrimaryScenario.ChannelId);
        await channels.UpdateItemAsync(channel with { IsSHIT = true });
        using var client = factory.CreateClient();

        using var detail = await client.GetAsync($"/api/matches/{match.Url}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var json = await detail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(count == 1 ? 0 : 1, json.GetProperty("contentType").GetInt32());
        Assert.Equal(count == 1, json.GetProperty("isLink").GetBoolean());
        Assert.False(json.TryGetProperty("effectiveContentType", out _));

        var v1 = await client.GetFromJsonAsync<JsonElement>($"/api/v1/videos/{match.Url}");
        Assert.Equal(count == 1 ? "SingleVideo" : "Collection", v1.GetProperty("contentType").GetString());

        var shooting = await client.GetFromJsonAsync<JsonElement>("/api/shit/matches");
        var shootingMatch = shooting.GetProperty("data").EnumerateArray()
            .Single(item => item.GetProperty("id").GetString() == match.Id);
        Assert.Equal(count == 1 ? 0 : 1, shootingMatch.GetProperty("contentType").GetInt32());
        Assert.Equal(count == 1, shootingMatch.GetProperty("isLink").GetBoolean());
        Assert.False(shootingMatch.TryGetProperty("effectiveContentType", out _));
        Assert.Equal(legacyType, (await repository.GetItemAsync(match.Id)).ContentType);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    public async Task Calendar_projects_explicit_reference_identity_without_modifying_persisted_calendar(
        int count, bool isPrivate)
    {
        await using var factory = new ServerApiWebApplicationFactory();
        var matches = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = await matches.GetItemAsync(PrimaryScenario.MatchId);
        var match = source with
        {
            ContentId = "not-a-video-id",
            ThumbnailVideoId = "unattached-thumbnail",
            ContentType = count == 1 ? YoutubeContentType.Collection : YoutubeContentType.SingleVideo,
            IsPrivate = isPrivate,
            VideoRefs = Enumerable.Range(0, count).Select(index =>
                new VideoRef($"attached-{index}", [], channelIds: [PrimaryScenario.ChannelId])).ToArray()
        };
        await matches.UpdateItemAsync(match);
        var calendars = factory.Services.GetRequiredService<ICalendarEventRepository>();
        var calendar = await calendars.AddItemAsync(CalendarSerializationTests.Sample() with
        {
            MatchId = match.Id, MatchUrl = string.Empty, Revision = 9, CreationDateTime = DateTime.UtcNow
        });
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<JsonElement>("/api/calendarEvents");
        var item = response.EnumerateArray().Single(value => value.GetProperty("id").GetString() == calendar.Id);
        if (count == 1 && !isPrivate)
        {
            Assert.Equal("attached-0", item.GetProperty("matchVideoId").GetString());
            Assert.Equal("attached-0", item.GetProperty("matchUrl").GetString());
        }
        else
        {
            Assert.Equal(JsonValueKind.Null, item.GetProperty("matchVideoId").ValueKind);
            Assert.Equal(isPrivate ? calendar.MatchUrl : match.Url, item.GetProperty("matchUrl").GetString());
        }
        Assert.False(item.TryGetProperty("revision", out _));
        Assert.Equal(calendar.MatchUrl, (await calendars.GetItemAsync(calendar.Id)).MatchUrl);
        Assert.Equal(calendar.Revision, (await calendars.GetItemAsync(calendar.Id)).Revision);
    }
}

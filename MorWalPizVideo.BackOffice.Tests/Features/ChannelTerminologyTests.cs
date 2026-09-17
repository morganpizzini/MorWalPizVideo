using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ChannelTerminologyTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public ChannelTerminologyTests(BackOfficeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Terminology_crud_is_channel_scoped_and_normalizes_values()
    {
        var channel = (await factory.YTChannelRepository!.GetItemsAsync()).Single(item => item.ChannelId == PrimaryScenario.ChannelId);
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.ChannelsManage);

        var initial = await client.GetAsync("/api/Channels/terminology");
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);

        var update = await client.PutAsJsonAsync("/api/Channels/terminology", new
        {
            italianToEnglish = new[] { new { source = "  colpo  ", target = "  shot " } },
            invariantEnglish = new[] { new { source = " Double Alpha ", target = "" } }
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var payload = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("colpo", payload.GetProperty("italianToEnglish")[0].GetProperty("source").GetString());
        Assert.Equal("shot", payload.GetProperty("italianToEnglish")[0].GetProperty("target").GetString());

        var stored = await factory.YTChannelRepository.GetItemAsync(channel.Id);
        Assert.Equal("colpo", stored!.Terminology.ItalianToEnglish[0].Source);

        var delete = await client.DeleteAsync("/api/Channels/terminology");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        stored = await factory.YTChannelRepository.GetItemAsync(channel.Id);
        Assert.Empty(stored!.Terminology.ItalianToEnglish);
    }

    [Fact]
    public async Task Terminology_rejects_duplicates_and_hides_other_channels()
    {
        var other = new YTChannel($"terminology-other-{Guid.NewGuid():N}", "Other terminology channel");
        await factory.YTChannelRepository!.AddItemAsync(other);
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.ChannelsManage);

        var invalid = await client.PutAsJsonAsync("/api/Channels/terminology", new
        {
            italianToEnglish = new[] { new { source = "same", target = "one" }, new { source = " SAME ", target = "two" } },
            invariantEnglish = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        client.DefaultRequestHeaders.Remove("X-Channel-Id");
        client.DefaultRequestHeaders.Add("X-Channel-Id", other.ChannelId);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/Channels/terminology")).StatusCode);
    }
}
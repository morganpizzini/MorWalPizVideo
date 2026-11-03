using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class VideoImporterSettingsEndpointTests(
    BackOfficeWebApplicationFactory factory) : IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task Settings_are_channel_scoped_and_put_is_idempotent()
    {
        var channelA = await AddChannelAsync($"importer-a-{Guid.NewGuid():N}");
        var channelB = await AddChannelAsync($"importer-b-{Guid.NewGuid():N}");
        using var clientA = await CreateClientAsync(channelA.ChannelId,
            AuthorizationPermissionKeys.VideoImporterSettingsRead,
            AuthorizationPermissionKeys.VideoImporterSettingsWrite);
        using var clientB = await CreateClientAsync(channelB.ChannelId,
            AuthorizationPermissionKeys.VideoImporterSettingsRead);

        var firstGet = await clientA.GetAsync("/api/v1/video-importer-settings");
        var payload = JsonDocument.Parse("""{"applicationName":"Importer A","languages":[{"code":"it","name":"Italiano"}]}""");
        var request = new VideoImporterChannelSettingsRequest(1, payload.RootElement.Clone());
        var firstPut = await clientA.PutAsJsonAsync("/api/v1/video-importer-settings", request);
        var secondPut = await clientA.PutAsJsonAsync("/api/v1/video-importer-settings", request);
        var getSaved = await clientA.GetAsync("/api/v1/video-importer-settings");
        var saved = await getSaved.Content.ReadFromJsonAsync<VideoImporterChannelSettingsResponse>();
        var otherChannelGet = await clientB.GetAsync("/api/v1/video-importer-settings");
        clientA.DefaultRequestHeaders.Remove("X-Channel-Id");
        clientA.DefaultRequestHeaders.Add("X-Channel-Id", channelB.ChannelId);
        var mismatchedKeyGet = await clientA.GetAsync("/api/v1/video-importer-settings");

        Assert.Equal(HttpStatusCode.NoContent, firstGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, firstPut.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondPut.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getSaved.StatusCode);
        Assert.Equal(channelA.ChannelId, saved!.ChannelId);
        Assert.Equal("Importer A", saved.Payload.GetProperty("applicationName").GetString());
        Assert.Equal(HttpStatusCode.NoContent, otherChannelGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, mismatchedKeyGet.StatusCode);

        var stored = await factory.VideoImporterChannelSettingsRepository!.GetByChannelIdAsync(channelA.ChannelId);
        Assert.NotNull(stored);
        Assert.Single(await factory.VideoImporterChannelSettingsRepository.GetItemsAsync(
            item => item.ChannelId == channelA.ChannelId));
    }

    [Fact]
    public async Task Settings_requires_channel_and_distinct_read_write_scopes()
    {
        var channel = await AddChannelAsync($"importer-auth-{Guid.NewGuid():N}");
        using var noChannel = await CreateClientAsync(channel.ChannelId,
            AuthorizationPermissionKeys.VideoImporterSettingsRead);
        noChannel.DefaultRequestHeaders.Remove("X-Channel-Id");
        using var noReadScope = await CreateClientAsync(channel.ChannelId,
            AuthorizationPermissionKeys.VideoImporterSettingsWrite);
        using var noWriteScope = await CreateClientAsync(channel.ChannelId,
            AuthorizationPermissionKeys.VideoImporterSettingsRead);

        var missingChannel = await noChannel.GetAsync("/api/v1/video-importer-settings");
        var deniedRead = await noReadScope.GetAsync("/api/v1/video-importer-settings");
        var deniedWrite = await noWriteScope.PutAsJsonAsync(
            "/api/v1/video-importer-settings",
            new VideoImporterChannelSettingsRequest(1, JsonDocument.Parse("{}").RootElement.Clone()));

        Assert.Equal(HttpStatusCode.BadRequest, missingChannel.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedRead.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);
    }

    [Fact]
    public async Task Settings_reject_unsupported_versions_and_non_object_payloads()
    {
        var channel = await AddChannelAsync($"importer-validation-{Guid.NewGuid():N}");
        using var client = await CreateClientAsync(channel.ChannelId, AuthorizationPermissionKeys.VideoImporterSettingsWrite);
        var unsupportedVersion = await client.PutAsJsonAsync(
            "/api/v1/video-importer-settings",
            new VideoImporterChannelSettingsRequest(2, JsonDocument.Parse("{}").RootElement.Clone()));
        var arrayPayload = await client.PutAsJsonAsync(
            "/api/v1/video-importer-settings",
            new VideoImporterChannelSettingsRequest(1, JsonDocument.Parse("[]").RootElement.Clone()));

        Assert.Equal(HttpStatusCode.BadRequest, unsupportedVersion.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, arrayPayload.StatusCode);
    }

    [Fact]
    public async Task Settings_reject_oversized_payloads_and_missing_api_keys()
    {
        var channel = await AddChannelAsync($"importer-size-{Guid.NewGuid():N}");
        using var client = await CreateClientAsync(channel.ChannelId, AuthorizationPermissionKeys.VideoImporterSettingsWrite);
        var oversizedPayload = JsonDocument.Parse(
            $$"""{"value":"{{new string('x', 64 * 1024 + 1)}}"}""");
        var oversizedResponse = await client.PutAsJsonAsync(
            "/api/v1/video-importer-settings",
            new VideoImporterChannelSettingsRequest(1, oversizedPayload.RootElement.Clone()));
        using var anonymousClient = factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Channel-Id", channel.ChannelId);
        var unauthenticatedResponse = await anonymousClient.GetAsync("/api/v1/video-importer-settings");

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversizedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedResponse.StatusCode);
    }

    private async Task<HttpClient> CreateClientAsync(string channelId, params string[] scopes)
    {
        var (apiKey, plaintextKey) = await factory.Services.GetRequiredService<IApiKeyService>()
            .CreateApiKeyAsync($"importer-settings-{Guid.NewGuid():N}", string.Empty, channelId, scopes: scopes.ToList());
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", plaintextKey);
        client.DefaultRequestHeaders.Add("X-Channel-Id", apiKey.ChannelId);
        return client;
    }

    private async Task<YTChannel> AddChannelAsync(string channelId)
    {
        var channel = new YTChannel(channelId, channelId);
        await factory.YTChannelRepository!.AddItemAsync(channel);
        return channel;
    }
}

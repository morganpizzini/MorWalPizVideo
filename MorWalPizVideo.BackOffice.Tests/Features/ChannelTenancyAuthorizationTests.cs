using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using Xunit;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ChannelTenancyAuthorizationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory _factory;

    public ChannelTenancyAuthorizationTests(BackOfficeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Scoped_requests_distinguish_missing_unknown_and_inaccessible_channel_headers()
    {
        using var missingHeaderClient = CreateClient(AuthorizationPermissionKeys.CompilationsView);
        using var unknownHeaderClient = CreateClient(AuthorizationPermissionKeys.CompilationsView, channelId: "missing-channel");
        var inaccessibleChannel = await AddChannelAsync($"inaccessible-{Guid.NewGuid():N}");
        using var inaccessibleHeaderClient = CreateClient(AuthorizationPermissionKeys.CompilationsView, channelId: inaccessibleChannel.ChannelId);

        Assert.Equal(HttpStatusCode.BadRequest, (await missingHeaderClient.GetAsync("/api/Compilations")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await unknownHeaderClient.GetAsync("/api/Compilations")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await inaccessibleHeaderClient.GetAsync("/api/Compilations")).StatusCode);
    }

    [Fact]
    public async Task Admin_can_select_and_manage_a_channel_other_than_the_default_channel()
    {
        var channel = await AddChannelAsync($"admin-channel-{Guid.NewGuid():N}");
        using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, channel.ChannelId);

        var response = await client.GetAsync($"/api/Channels/{channel.ChannelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_reassign_an_api_key_to_any_existing_channel_but_owner_cannot()
    {
        var targetChannel = await AddChannelAsync($"key-target-{Guid.NewGuid():N}");
        var apiKey = await AddApiKeyAsync(new ApiKey
        {
            Id = $"key-{Guid.NewGuid():N}",
            Name = $"key-{Guid.NewGuid():N}",
            ChannelId = PrimaryScenario.ChannelId,
            Key = "hash"
        });

        using var adminClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);
        var adminResponse = await adminClient.PutAsJsonAsync($"/api/ApiKeys/{apiKey.Id}", new { channelId = targetChannel.ChannelId });
        var updated = await _factory.Services.GetRequiredService<IApiKeyRepository>().GetItemAsync(apiKey.Id!);

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal(targetChannel.ChannelId, updated!.ChannelId);

        var ownerKey = await AddApiKeyAsync(new ApiKey
        {
            Id = $"owner-key-{Guid.NewGuid():N}",
            Name = $"owner-key-{Guid.NewGuid():N}",
            ChannelId = PrimaryScenario.ChannelId,
            Key = "hash"
        });
        using var ownerClient = CreateClient(AuthorizationPermissionKeys.ApiKeysUpdate, PrimaryScenario.ChannelId);
        var ownerResponse = await ownerClient.PutAsJsonAsync($"/api/ApiKeys/{ownerKey.Id}", new { channelId = targetChannel.ChannelId });

        Assert.Equal(HttpStatusCode.NotFound, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task Repository_backed_api_key_is_bound_to_its_channel_and_follows_admin_reassignment()
    {
        var boundChannel = await AddChannelAsync($"key-bound-{Guid.NewGuid():N}");
        var targetChannel = await AddChannelAsync($"key-reassigned-{Guid.NewGuid():N}");
        var plaintextKey = $"test-api-key-{Guid.NewGuid():N}";
        var apiKey = await AddApiKeyAsync(new ApiKey
        {
            Id = $"key-{Guid.NewGuid():N}",
            Name = $"key-{Guid.NewGuid():N}",
            Key = _factory.Services.GetRequiredService<IApiKeyService>().HashApiKey(plaintextKey),
            ChannelId = boundChannel.ChannelId
        });

        using var boundClient = CreateClient(AuthorizationPermissionKeys.ApiKeysView, boundChannel.ChannelId);
        boundClient.DefaultRequestHeaders.Add("X-Test-ApiKey", plaintextKey);
        using var boundResponse = await boundClient.GetAsync("/api/ApiKeys");

        using var mismatchClient = CreateClient(AuthorizationPermissionKeys.ApiKeysView, targetChannel.ChannelId);
        mismatchClient.DefaultRequestHeaders.Add("X-Test-ApiKey", plaintextKey);
        using var mismatchResponse = await mismatchClient.GetAsync("/api/ApiKeys");

        using var adminClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, boundChannel.ChannelId);
        using var reassignmentResponse = await adminClient.PutAsJsonAsync(
            $"/api/ApiKeys/{apiKey.Id}",
            new { channelId = targetChannel.ChannelId });

        using var targetClient = CreateClient(AuthorizationPermissionKeys.ApiKeysView, targetChannel.ChannelId);
        targetClient.DefaultRequestHeaders.Add("X-Test-ApiKey", plaintextKey);
        using var targetResponse = await targetClient.GetAsync("/api/ApiKeys");

        Assert.Equal(HttpStatusCode.OK, boundResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, mismatchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reassignmentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, targetResponse.StatusCode);
    }

    [Fact]
    public async Task Collaborator_can_read_another_channel_video_but_cannot_mutate_it()
    {
        var collaboratorChannel = await AddChannelAsync($"collaborator-{Guid.NewGuid():N}");
        await AddOwnerAsync("test-user-id", collaboratorChannel.ChannelId);
        var videoId = $"collaborator-video-{Guid.NewGuid():N}";
        var match = await _factory.MatchRepository!.AddItemAsync(
            YouTubeContent.CreateSingleVideo(videoId, []) with
            {
                Id = $"match-{Guid.NewGuid():N}",
                OwnerChannelId = PrimaryScenario.ChannelId,
                CreatorUserId = "another-user",
                VideoRefs = [new VideoRef(videoId, [], channelIds: [collaboratorChannel.ChannelId])]
            });

        using var client = CreateClient(AuthorizationPermissionKeys.VideosView, collaboratorChannel.ChannelId);
        var readResponse = await client.GetAsync($"/api/Videos/{match.Id}");
        using var mutationClient = CreateClient(AuthorizationPermissionKeys.VideosUpdate, collaboratorChannel.ChannelId);
        var mutationResponse = await mutationClient.PutAsJsonAsync($"/api/Videos/{match.Id}", new
        {
            title = "not allowed",
            description = string.Empty,
            url = string.Empty,
            thumbnailVideoId = videoId,
            categories = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, mutationResponse.StatusCode);
    }

    [Fact]
    public async Task Compilation_owner_can_include_a_video_readable_through_another_owned_channel()
    {
        var sourceChannel = await AddChannelAsync($"source-{Guid.NewGuid():N}");
        await AddOwnerAsync("test-user-id", sourceChannel.ChannelId);
        var videoId = $"cross-channel-video-{Guid.NewGuid():N}";
        await _factory.MatchRepository!.AddItemAsync(
            YouTubeContent.CreateSingleVideo(videoId, []) with
            {
                Id = $"match-{Guid.NewGuid():N}",
                OwnerChannelId = sourceChannel.ChannelId,
                CreatorUserId = "another-user",
                VideoRefs = [new VideoRef(videoId, [], channelIds: [sourceChannel.ChannelId])]
            });

        using var client = CreateClient(AuthorizationPermissionKeys.CompilationsCreate, PrimaryScenario.ChannelId);
        var response = await client.PostAsJsonAsync("/api/Compilations", new
        {
            title = "Cross-channel compilation",
            description = "Readable source video",
            url = $"cross-channel-{Guid.NewGuid():N}",
            videos = new[] { videoId }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private HttpClient CreateClient(string permission, string? channelId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permission);
        if (channelId is not null)
        {
            client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        }

        return client;
    }

    private async Task<YTChannel> AddChannelAsync(string channelId)
        => await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, channelId));

    private async Task AddOwnerAsync(string userId, string channelId)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserChannelOwnerRepository>();
        await repository.AddItemAsync(new UserChannelOwner
        {
            UserId = userId,
            ChannelId = channelId,
            IsActive = true
        });
    }

    private async Task<ApiKey> AddApiKeyAsync(ApiKey apiKey)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
        return await repository.AddItemAsync(apiKey);
    }
}
